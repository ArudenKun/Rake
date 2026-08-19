using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using R3.ObservableEvents;
using Rake.ViewModels;
using SukiUI.Controls;

namespace Rake;

public static class GlobalExceptionHandler
{
    [ThreadStatic]
    private static bool _showingError;
    private static ILogger? _logger;
    private static bool _isInstalled;

    public static void Install(ILogger? logger = null)
    {
        if (Debugger.IsAttached)
            return;

        if (_isInstalled)
            return;

        _logger = logger ?? NullLogger.Instance;

        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandled;
        TaskScheduler.UnobservedTaskException += OnUnobservedTask;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandled;

        _isInstalled = true;
    }

    public static void Show(Exception exception) => Report(exception);

    private static void OnAppDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Report(ex);
    }

    private static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Ignore cancellations triggered by MVVM Toolkit AsyncRelayCommand or token teardowns
        if (IsCancellationException(e.Exception))
        {
            e.SetObserved();
            return;
        }

        if (sender is ViewModel)
            return;

        e.SetObserved();
        Report(e.Exception);
    }

    private static void OnDispatcherUnhandled(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    )
    {
        // Suppress TaskCanceledException/OperationCanceledException from bubbling up to UI dialog
        if (IsCancellationException(e.Exception))
        {
            e.Handled = true;
            return;
        }

        Report(e.Exception);
        e.Handled = true;
    }

    private static void Report(Exception exception)
    {
        // Filter out direct or unwrapped OperationCanceledException/TaskCanceledException
        var unwrapped = UnwrapException(exception);
        if (IsCancellationException(unwrapped))
            return;

        Debug.WriteLine(unwrapped.ToString());
        _logger?.LogException(unwrapped);

        if (_showingError)
            return;

        _showingError = true;
        try
        {
            ShowDialog(unwrapped);
        }
        finally
        {
            _showingError = false;
        }
    }

    private static Exception UnwrapException(Exception exception)
    {
        var current = exception;

        // Unwrap AggregateException if it contains a inner concrete exception
        if (current is AggregateException { InnerExceptions.Count: 1 } aggErr)
        {
            current = aggErr.InnerException!;
        }

        for (var ex = current; ex != null; ex = ex.InnerException)
        {
            if (
                ex is ReflectionTypeLoadException { LoaderExceptions.Length: > 0 } loadException
                && loadException.LoaderExceptions[0] is { } loader
            )
            {
                return loader;
            }
        }

        return current;
    }

    private static bool IsCancellationException(Exception? ex)
    {
        if (ex is null)
            return false;

        if (ex is OperationCanceledException or TaskCanceledException)
            return true;

        if (ex is AggregateException agg)
        {
            return agg.InnerExceptions.Count > 0
                && agg.InnerExceptions.All(e =>
                    e is OperationCanceledException or TaskCanceledException
                );
        }

        return false;
    }

    private static void ShowDialog(Exception exception)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            try
            {
                Dispatcher.UIThread.Post(() => ShowDialog(exception));
            }
            catch
            {
                // Dispatcher already torn down — nothing to do.
            }
            return;
        }

        var window = new SukiWindow
        {
            Title = "Rake — Unhandled Exception",
            Width = 720,
            Height = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var clipboardText = FormatForClipboard(exception);
        var (root, details) = BuildContent(exception, window, clipboardText);
        window.Content = root;

        window.KeyBindings.Add(
            new KeyBinding
            {
                Gesture = new KeyGesture(Key.C, KeyModifiers.Control),
                Command = new AsyncRelayCommand(
                    () => CopyToClipboardAsync(window, clipboardText),
                    () => string.IsNullOrEmpty(details.SelectedText)
                ),
            }
        );
        window.KeyBindings.Add(
            new KeyBinding
            {
                Gesture = new KeyGesture(Key.Escape),
                Command = new RelayCommand(window.Close),
            }
        );

        Window? owner = null;
        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
        )
            owner = desktop.MainWindow;

        if (owner is { IsVisible: true })
#pragma warning disable VSTHRD110
            window.ShowDialog(owner);
#pragma warning restore VSTHRD110
        else
            window.Show();
    }

    private static (Control root, TextBox details) BuildContent(
        Exception exception,
        Window window,
        string clipboardText
    )
    {
        var detailText = exception.ToString();
        var details = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
            FontSize = 12,
            Text = detailText,
        };

        var summary = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 8),
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
            Text = exception.GetType().FullName + ": " + exception.Message,
        };

        var disposables = new DisposableBag();

        var copy = new Button { Content = "Copy" };
        copy.Events()
            .PointerExited.ObserveOnUIThreadDispatcher()
            .Subscribe(_ =>
            {
                ToolTip.SetIsOpen(copy, false);
                ToolTip.SetTip(copy, null);
            })
            .AddTo(ref disposables);
        copy.Events()
            .Click.ObserveOnUIThreadDispatcher()
            .SubscribeAwait(
                async (_, ct) =>
                {
                    ToolTip.SetTip(copy, "Copied");
                    ToolTip.SetIsOpen(copy, true);

                    await CopyToClipboardAsync(window, clipboardText);

                    await Task.Delay(1500, ct);

                    ToolTip.SetIsOpen(copy, false);
                    ToolTip.SetTip(copy, null);
                }
            )
            .AddTo(ref disposables);

        var dismiss = new Button { Content = "Close", IsDefault = true };
        dismiss
            .Events()
            .Click.ObserveOnUIThreadDispatcher()
            .Subscribe(_ => window.Close())
            .AddTo(ref disposables);

        window.Closed += (_, _) => disposables.Dispose();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0),
            Spacing = 8,
            Children = { copy, dismiss },
        };

        var grid = new Grid
        {
            Margin = new Thickness(12),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
        };
        Grid.SetRow(summary, 0);
        Grid.SetRow(details, 1);
        Grid.SetRow(buttons, 2);
        grid.Children.Add(summary);
        grid.Children.Add(details);
        grid.Children.Add(buttons);
        return (grid, details);
    }

    private static string FormatForClipboard(Exception exception)
    {
        return exception.GetType().FullName
            + ": "
            + exception.Message
            + Environment.NewLine
            + exception;
    }

    private static async Task CopyToClipboardAsync(Window window, string text)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard is null)
            return;
        await clipboard.SetTextAsync(text);
    }
}

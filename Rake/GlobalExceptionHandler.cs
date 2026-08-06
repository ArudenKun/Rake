using System;
using System.Diagnostics;
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

    /// <summary>
    /// Surfaces <paramref name="exception"/> through the same UI as an unhandled exception.
    /// Lets call sites that swallow exceptions in a fire-and-forget continuation still report
    /// them through the standard dialog.
    /// </summary>
    public static void Show(Exception exception) => Report(exception);

    static void OnAppDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Report(ex);
    }

    static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (sender is ViewModel)
            return;

        e.SetObserved();
        // The Linux DBus failures arrive here: a fire-and-forget Tmds.DBus call faults and the
        // AggregateException is rethrown by the finalizer. Log the unwrapped chain plus the
        // recent input trail so the triggering gesture can be read off (ILSPY_LOG=DBUSDEBUG).
        Report(e.Exception);
    }

    static void OnDispatcherUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Report(e.Exception);
        e.Handled = true;
    }

    static void Report(Exception exception)
    {
        Debug.WriteLine(exception.ToString());
        _logger?.LogException(exception);
        for (var ex = exception; ex != null; ex = ex.InnerException)
        {
            if (
                ex is ReflectionTypeLoadException { LoaderExceptions.Length: > 0 } loadException
                && loadException.LoaderExceptions[0] is { } loader
            )
            {
                exception = loader;
                Debug.WriteLine(exception.ToString());
                _logger?.LogException(exception);
                break;
            }
        }

        if (_showingError)
            return;
        _showingError = true;
        try
        {
            ShowDialog(exception);
        }
        finally
        {
            _showingError = false;
        }
    }

    static void ShowDialog(Exception exception)
    {
        // Marshal to the UI thread; nested calls during shutdown may not have a dispatcher,
        // in which case Debug.WriteLine above is the only signal we can offer.
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
        // Match the Win32 MessageBox keyboard contract: Ctrl+C copies the whole report,
        // Esc dismisses the dialog. Skip the whole-text copy when the user has a selection
        // inside the details TextBox so the standard text-selection copy wins.
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
            window.ShowDialog(owner);
        else
            window.Show();
    }

    static (Control root, TextBox details) BuildContent(
        Exception exception,
        Window window,
        string clipboardText
    )
    {
        // For DBus failures the bare ToString() hides the ServiceUnknown/ErrorName detail inside
        // nested aggregates; append the fully unwrapped chain so the dialog carries it too.
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

    static string FormatForClipboard(Exception exception)
    {
        var text =
            exception.GetType().FullName
            + ": "
            + exception.Message
            + Environment.NewLine
            + exception;
        return text;
    }

    static async Task CopyToClipboardAsync(Window window, string text)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard is null)
            return;
        await clipboard.SetTextAsync(text);
    }
}

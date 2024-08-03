using System;
using System.Threading;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Rake.ViewModels;
using Rake.Views;
using Ursa.Controls;

namespace Rake.Services;

[AutoInterface]
public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessenger _messenger;

    private readonly SemaphoreSlim _dialogLock = new(1, 1);

    public DialogService(IServiceProvider serviceProvider, IMessenger messenger)
    {
        _serviceProvider = serviceProvider;
        _messenger = messenger;

        Dialog.ShowModal<MainWindow, MainWindowViewModel>(
            serviceProvider.GetRequiredService<MainWindowViewModel>()
        );
    }

    public Task<MessageBoxResult> ShowMessageBoxAsync(
        string title,
        string message,
        bool showOverlay,
        MessageBoxIcon messageBoxIcon,
        MessageBoxButton messageBoxButton
    ) =>
        showOverlay
            ? MessageBox.ShowOverlayAsync(
                message,
                title,
                icon: messageBoxIcon,
                button: messageBoxButton
            )
            : MessageBox.ShowAsync(message, title, messageBoxIcon, messageBoxButton);
}

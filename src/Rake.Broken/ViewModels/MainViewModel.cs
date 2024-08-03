using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rake.Extensions;
using Rake.Services;
using Rake.ViewModels.Common;
using Ursa.Controls;

namespace Rake.ViewModels;

public sealed partial class MainViewModel : BaseViewModel
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _closeOnClickAway;

    public MainViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task ShowMessageBox()
    {
        var result = await _dialogService
            .ShowMessageBoxAsync(
                "A new Update",
                "Test",
                CloseOnClickAway,
                MessageBoxIcon.Information,
                MessageBoxButton.OK
            )
            .ConfigureAwait(true);
        Console.WriteLine($"Result: {result.GetName()}");
    }

    [RelayCommand]
    private void ChangeCloseOnClickAway()
    {
        CloseOnClickAway = !CloseOnClickAway;
    }
}

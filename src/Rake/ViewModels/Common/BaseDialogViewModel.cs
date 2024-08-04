using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rake.ViewModels.Common;

public abstract partial class BaseDialogViewModel<TResult> : BaseViewModel
{
    private readonly TaskCompletionSource<TResult> _closeTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    [ObservableProperty]
    private TResult? _dialogResult;

    [RelayCommand]
    private void Close(TResult dialogResult)
    {
        DialogResult = dialogResult;
        _closeTcs.TrySetResult(dialogResult);
    }

    public async Task<TResult> WaitForCloseAsync() => await _closeTcs.Task;
}

public abstract class BaseDialogViewModel : BaseDialogViewModel<bool>;

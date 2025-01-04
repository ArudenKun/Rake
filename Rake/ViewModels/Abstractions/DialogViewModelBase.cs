using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rake.ViewModels.Abstractions;

public abstract class DialogViewModelBase : DialogViewModelBase<bool>;

public abstract partial class DialogViewModelBase<TResult> : ViewModelBase
{
    private readonly TaskCompletionSource<TResult> _closeTcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    [ObservableProperty]
    private TResult? _dialogResult;

    [RelayCommand]
    protected void Close(TResult dialogResult)
    {
        DialogResult = dialogResult;
        _closeTcs.TrySetResult(dialogResult);
    }

    public async Task<TResult> WaitForCloseAsync() => await _closeTcs.Task;
}

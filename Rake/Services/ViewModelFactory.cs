using System;
using Microsoft.Extensions.DependencyInjection;
using Rake.ViewModels.Dialogs;
using SukiUI.Dialogs;

namespace Rake.Services;

public sealed class ViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public UpdateViewModel CreateUpdateViewModel(ISukiDialog dialog)
    {
        var viewModel = _serviceProvider.GetRequiredService<UpdateViewModel>();
        viewModel.Dialog = dialog;
        return viewModel;
    }
}

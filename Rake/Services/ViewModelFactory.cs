using System;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Rake.ViewModels;
using Velopack;

namespace Rake.Services;

public sealed class ViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public UpdateViewModel CreateUpdateViewModel(
        VelopackAsset updatePackage,
        SemanticVersion currentVersion
    )
    {
        var viewModel = _serviceProvider.GetRequiredService<UpdateViewModel>();
        viewModel.CurrentVersion = currentVersion;
        viewModel.NewVersion = updatePackage.Version;
        viewModel.UpdatePackage = updatePackage;
        viewModel.FileSize = updatePackage.Size.Bytes().Humanize();
        return viewModel;
    }
}

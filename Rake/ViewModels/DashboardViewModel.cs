using NuGet.Versioning;
using Rake.Services;
using Rake.ViewModels.Abstractions;

namespace Rake.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;

    public DashboardViewModel(UpdateService updateService)
    {
        _updateService = updateService;
    }

    public SemanticVersion CurrentVersion => _updateService.CurrentVersion;
}

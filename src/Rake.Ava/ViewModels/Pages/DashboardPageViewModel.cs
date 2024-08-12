using Material.Icons;
using Rake.ViewModels.Abstractions;

namespace Rake.ViewModels.Pages;

public sealed class DashboardPageViewModel : BasePageViewModel
{
    public override int PageIndex => 1;
    public override MaterialIconKind PageIconKind => MaterialIconKind.Home;
}

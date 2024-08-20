using Material.Icons;
using Rake.ViewModels.Abstractions;

namespace Rake.ViewModels.Pages;

public sealed class SettingsPageViewModel : BasePageViewModel
{
    public override int PageIndex => 99999;
    public override MaterialIconKind PageIconKind => MaterialIconKind.Settings;
}

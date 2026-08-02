using Riok.Mapperly.Abstractions;

namespace Rake.Settings;

[Mapper]
public static partial class SettingExtensions
{
    public static partial void ApplyUpdate(this Setting source, Setting update);
}

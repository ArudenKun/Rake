namespace Rake.Controls.WebView;

public record WebViewSettings
{
    public bool AreDevToolEnabled { get; set; } = true;

    public bool AreDefaultContextMenusEnabled { get; set; } = false;

    public bool IsStatusBarEnabled { get; set; } = false;

    public string? BrowserExecutableFolder { get; set; } = default;

    public string? UserDataFolder { get; set; } = default;

    public string? Language { get; set; } = default;

    public string? AdditionalBrowserArguments { get; set; } = default;

    public string? ProfileName { get; set; } = default;

    public bool? IsInPrivateModeEnabled { get; set; } = default;
}

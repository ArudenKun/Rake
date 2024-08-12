using System;

namespace Rake.Controls.WebView;

public class WebViewNavigationEventArgs : EventArgs
{
    public Uri? Request { get; init; }
}

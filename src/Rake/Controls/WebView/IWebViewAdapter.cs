using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;

namespace Rake.Controls.WebView;

public interface IWebViewAdapter : IWebView, IDisposable, IPlatformHandle
{
    Task SetParentAsync(IntPtr handle);

    void HandleSizeChanged(Size newSize);
}

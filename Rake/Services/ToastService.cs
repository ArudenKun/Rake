using SukiUI.Toasts;

namespace Rake.Services;

public class ToastService
{
    public ToastService(ISukiToastManager manager)
    {
        Manager = manager;
    }

    public ISukiToastManager Manager { get; }
}

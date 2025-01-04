using System;
using SukiUI.Toasts;

namespace Rake.Extensions;

public static class DismissToastExtensions
{
    public static SukiToastBuilder After(
        this SukiToastBuilder.DismissToast dismiss,
        int afterSeconds
    )
    {
        dismiss.After(TimeSpan.FromSeconds(afterSeconds));
        return dismiss.Builder;
    }
}

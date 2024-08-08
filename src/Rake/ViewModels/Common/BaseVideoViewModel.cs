using LibVLCSharp.Shared;
using Rake.Core.Extensions;

namespace Rake.ViewModels.Common;

public abstract class BaseVideoViewModel : BaseViewModel
{
    public BaseVideoViewModel()
    {
        LibVlc = new LibVLC().DisposeWith(Disposables);
        MediaPlayer = new MediaPlayer(LibVlc).DisposeWith(Disposables);
    }

    protected LibVLC LibVlc { get; }
    public MediaPlayer MediaPlayer { get; }
}

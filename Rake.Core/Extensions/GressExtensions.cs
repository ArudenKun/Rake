using Gress;
using Rake.Core.Downloading;

namespace Rake.Core.Extensions;

public static class GressExtensions
{
    // public static CopyProgressMuxer CreateMuxer(this IProgress<ICopyProgress> progress) =>
    //     new(progress);

    public static IProgress<ICopyProgress>? Wrap(this IProgress<Percentage>? progress)
    {
        IProgress<ICopyProgress>? prog = null;
        if (progress is not null)
        {
            prog = new Progress<ICopyProgress>(copyProgress =>
            {
                progress.Report(copyProgress.Percentage);
            });
        }
        return prog;
    }

    public static IProgress<ICopyProgress>? Wrap(this IProgress<double>? progress)
    {
        IProgress<ICopyProgress>? prog = null;
        if (progress is not null)
        {
            prog = new Progress<ICopyProgress>(copyProgress =>
            {
                progress.Report(copyProgress.Percentage.Fraction);
            });
        }
        return prog;
    }
}

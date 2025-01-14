using System.Threading.Tasks;
using Quartz;

namespace Rake.Jobs;

public class DownloadVideoJob : IRakeJob
{
    public static string Name => nameof(DownloadVideoJob);

    public Task Execute(IJobExecutionContext context)
    {
        throw new System.NotImplementedException();
    }
}

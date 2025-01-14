using Quartz;

namespace Rake.Jobs;

public interface IRakeJob : IJob
{
    static abstract string Name { get; }
}

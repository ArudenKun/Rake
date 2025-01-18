using Microsoft.Extensions.Logging;
using Rake.Hosting.Abstractions;

namespace Rake.Hosting.Mutex;

/// <summary>
/// This is the configuration for the mutex service
/// </summary>
internal class MutexBuilder : IMutexBuilder
{
    /// <inheritdoc />
    public string MutexId { get; set; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public Func<IServiceProvider, ILogger, CancellationToken, Task>? WhenFirstInstance { get; set; }

    /// <inheritdoc />
    public Func<
        IServiceProvider,
        ILogger,
        CancellationToken,
        Task
    >? WhenNotFirstInstance { get; set; }
}

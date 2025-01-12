using Microsoft.Extensions.Logging;

namespace Rake.Hosting.Abstractions;

/// <summary>
/// This is to configure the ForceSingleInstance extension
/// </summary>
public interface IMutexBuilder
{
    /// <summary>
    /// The name of the mutex, usually a GUID
    /// </summary>
    string MutexId { get; set; }

    /// <summary>
    /// The action which is called when the mutex can be locked
    /// </summary>
    Func<IServiceProvider, ILogger, CancellationToken, Task>? WhenFirstInstance { get; set; }

    /// <summary>
    /// The action which is called when the mutex cannot be locked
    /// </summary>
    Func<IServiceProvider, ILogger, CancellationToken, Task>? WhenNotFirstInstance { get; set; }
}

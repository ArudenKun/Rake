using Avalonia;
using Avalonia.Threading;
using Rake.Hosting.Abstractions;

namespace Rake.Hosting;

/// <summary>
/// Encapsulates the information needed to manage the hosting of an Avalonia based
/// User Interface service and associated thread.
/// </summary>
public class HostingContext(bool lifeTimeLinked = true) : BaseHostingContext(lifeTimeLinked)
{
    /// <summary>Gets or sets the Avalonia dispatcher.</summary>
    /// <value>The Avalonia dispatcher.</value>
    public Dispatcher? Dispatcher { get; set; }

    /// <summary>Gets or sets the Avalonia Application instance.</summary>
    /// <value>The Avalonia Application instance.</value>
    public Application? Application { get; set; }
}

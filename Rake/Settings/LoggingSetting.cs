using CommunityToolkit.Mvvm.ComponentModel;
using Serilog.Events;

namespace Rake.Settings;

public class LoggingSetting : ObservableObject
{
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;
}

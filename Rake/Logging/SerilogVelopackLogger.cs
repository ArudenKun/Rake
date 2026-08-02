using System;
using Serilog;
using Serilog.Events;
using Velopack.Logging;

namespace Rake.Logging;

public class SerilogVelopackLogger : IVelopackLogger
{
    private readonly ILogger _logger;

    public SerilogVelopackLogger(ILogger? logger = null)
    {
        // Fallback to the g
        // lobal static Serilog logger if none is injected
        _logger = logger ?? Serilog.Log.ForContext<SerilogVelopackLogger>();
    }

    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        // 1. Map VelopackLogLevel to Serilog LogEventLevel
        var serilogLevel = MapLogLevel(logLevel);

        // 2. Check if this level is enabled to avoid unnecessary processing
        if (!_logger.IsEnabled(serilogLevel))
            return;

        // 3. Dispatch to Serilog
        if (exception is not null)
        {
            _logger.Write(
                serilogLevel,
                exception,
                "{VelopackMessage}",
                message ?? exception.Message
            );
        }
        else if (!string.IsNullOrEmpty(message))
        {
            _logger.Write(serilogLevel, "{VelopackMessage}", message);
        }
    }

    private static LogEventLevel MapLogLevel(VelopackLogLevel level) =>
        level switch
        {
            VelopackLogLevel.Trace => LogEventLevel.Verbose,
            VelopackLogLevel.Debug => LogEventLevel.Debug,
            VelopackLogLevel.Information => LogEventLevel.Information,
            VelopackLogLevel.Warning => LogEventLevel.Warning,
            VelopackLogLevel.Error => LogEventLevel.Error,
            VelopackLogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(
                nameof(level),
                "VelopackLogLevel is not valid"
            ),
        };
}

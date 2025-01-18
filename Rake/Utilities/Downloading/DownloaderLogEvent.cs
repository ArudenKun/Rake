using System;

namespace Rake.Utilities.Downloading;

public enum DownloaderLogSeverity
{
    Info,
    Error,
    Warning,
}

public class DownloaderLogEvent
{
    public string Message { get; private set; }
    public DownloaderLogSeverity Severity { get; private set; }

    private DownloaderLogEvent(string message, DownloaderLogSeverity severity)
    {
        Message = message;
        Severity = severity;
    }

    // Download Progress Event Handler
    public static event EventHandler<DownloaderLogEvent>? DownloadEvent;

    // Push log to listener
    public static void PushLog(string message, DownloaderLogSeverity severity)
    {
        DownloadEvent?.Invoke(null, new DownloaderLogEvent(message, severity));
    }
}

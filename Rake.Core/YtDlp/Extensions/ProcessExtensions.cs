using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rake.Core.YtDlp.Extensions;

/// <summary>
/// Process extensions for killing full process tree.
/// </summary>
internal static class ProcessExtensions
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static void KillTree(this Process process)
    {
        process.KillTree(DefaultTimeout);
    }

    public static void KillTree(this Process process, TimeSpan timeout)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RunProcessAndWaitForExit("taskkill", $"/T /F /PID {process.Id}", timeout, out _);
        }
        else
        {
            var children = new HashSet<int>();
            GetAllChildIdsUnix(process.Id, children, timeout);
            foreach (var childId in children)
            {
                KillProcessUnix(childId, timeout);
            }
            KillProcessUnix(process.Id, timeout);
        }
    }

    private static void GetAllChildIdsUnix(int parentId, ISet<int> children, TimeSpan timeout)
    {
        var exitCode = RunProcessAndWaitForExit("pgrep", $"-P {parentId}", timeout, out var stdout);

        if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
        {
            using var reader = new StringReader(stdout);
            while (true)
            {
                var text = reader.ReadLine();
                if (text == null)
                {
                    return;
                }

                if (int.TryParse(text, out var id))
                {
                    children.Add(id);
                    GetAllChildIdsUnix(id, children, timeout);
                }
            }
        }
    }

    private static void KillProcessUnix(int processId, TimeSpan timeout)
    {
        RunProcessAndWaitForExit("kill", $"-TERM {processId}", timeout, out _);
    }

    private static int RunProcessAndWaitForExit(
        string fileName,
        string arguments,
        TimeSpan timeout,
        out string stdout
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = Process.Start(startInfo)!;

        stdout = string.Empty;
        if (process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            stdout = process.StandardOutput.ReadToEnd();
        }
        else
        {
            process.Kill();
        }

        return process.ExitCode;
    }
}

using System.Diagnostics;
using Termina.Diagnostics;

namespace Termina.Clipboard;

internal sealed class TmuxClipboardTransport : IClipboardTransport
{
    public string Name => "tmux";

    public bool CanHandle() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TMUX"));

    public bool TryCopy(string text)
    {
        try
        {
            var startInfo = new ProcessStartInfo("tmux")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("set-buffer");
            startInfo.ArgumentList.Add("-w");
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add(text);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                TerminaTrace.Platform.Warning(this, "tmux clipboard transport could not start");
                return false;
            }

            process.WaitForExit(1000);
            if (!process.HasExited)
            {
                TerminaTrace.Platform.Warning(this, "tmux clipboard transport timed out");
                try
                {
                    process.Kill();
                }
                catch
                {
                }

                return false;
            }

            if (process.ExitCode == 0)
            {
                TerminaTrace.Platform.Debug(this, "tmux clipboard transport completed successfully");
                return true;
            }

            var error = process.StandardError.ReadToEnd();
            TerminaTrace.Platform.Warning(this, "tmux clipboard transport failed: exitCode={0}, error={1}", process.ExitCode, error);
            return false;
        }
        catch (Exception ex)
        {
            TerminaTrace.Platform.Warning(this, "tmux clipboard transport threw: {0}", ex.Message);
            return false;
        }
    }
}

using Termina.Diagnostics;

namespace Termina.Clipboard;

internal sealed class TerminalClipboardService : IClipboardService
{
    private readonly IReadOnlyList<IClipboardTransport> _transports;

    public TerminalClipboardService(IEnumerable<IClipboardTransport> transports)
    {
        _transports = transports.ToList();
    }

    public bool Copy(string text)
    {
        TerminaTrace.Platform.Info(this, "Clipboard copy requested: textLength={0}", text.Length);

        var attempted = false;
        var successful = false;

        foreach (var transport in _transports)
        {
            if (!transport.CanHandle())
                continue;

            attempted = true;
            var copied = transport.TryCopy(text);
            successful |= copied;
            TerminaTrace.Platform.Debug(this, "Clipboard transport {0} attempted, success={1}", transport.Name, copied);
        }

        if (!attempted)
        {
            TerminaTrace.Platform.Warning(this, "No clipboard transports were available");
        }
        else
        {
            TerminaTrace.Platform.Info(this, "Clipboard transports completed: success={0}", successful);
        }

        return successful;
    }
}

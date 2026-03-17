using Termina.Diagnostics;
using Termina.Terminal;

namespace Termina.Clipboard;

internal sealed class Osc52ClipboardTransport : IClipboardTransport
{
    private readonly IAnsiTerminal _terminal;

    public Osc52ClipboardTransport(IAnsiTerminal terminal)
    {
        _terminal = terminal;
    }

    public string Name => "osc52";

    public bool CanHandle() => true;

    public bool TryCopy(string text)
    {
        TerminaTrace.Platform.Info(this, "OSC52 clipboard transport: textLength={0}", text.Length);
        _terminal.CopyToClipboard(text);
        _terminal.Flush();
        TerminaTrace.Platform.Debug(this, "OSC52 clipboard transport flushed");
        return true;
    }
}

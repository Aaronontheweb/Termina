using Termina.Notifications;
using Termina.Terminal;

namespace Termina.Clipboard;

internal sealed class TerminalClipboardService : IClipboardService
{
    private readonly IAnsiTerminal _terminal;
    private readonly IToastService _toastService;

    public TerminalClipboardService(IAnsiTerminal terminal, IToastService toastService)
    {
        _terminal = terminal;
        _toastService = toastService;
    }

    public void Copy(string text)
    {
        _terminal.CopyToClipboard(text);
        _toastService.Show("Copied to clipboard");
    }
}

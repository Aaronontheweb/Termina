namespace Termina.Clipboard;

/// <summary>
/// Copies text to the user's clipboard using the active terminal.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copy the provided text to the clipboard.
    /// </summary>
    void Copy(string text);
}

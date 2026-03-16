namespace Termina.Clipboard;

/// <summary>
/// A clipboard transport capable of sending copy requests through a specific channel.
/// </summary>
internal interface IClipboardTransport
{
    /// <summary>
    /// Human-readable transport name for tracing.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this transport applies in the current environment.
    /// </summary>
    bool CanHandle();

    /// <summary>
    /// Attempt to copy the provided text. Returns true on known success.
    /// </summary>
    bool TryCopy(string text);
}

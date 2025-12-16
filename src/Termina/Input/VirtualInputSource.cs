using System.Threading.Channels;

namespace Termina.Input;

/// <summary>
/// Virtual input source for testing and programmatic input.
/// External code pushes events via Enqueue methods, which get forwarded to the event loop.
/// </summary>
public sealed class VirtualInputSource : IInputSource
{
    private readonly Channel<IInputEvent> _inputChannel = Channel.CreateUnbounded<IInputEvent>();

    /// <summary>
    /// Enqueue a key event to be processed.
    /// </summary>
    public void EnqueueKey(ConsoleKeyInfo key)
    {
        _inputChannel.Writer.TryWrite(new KeyPressed(key));
    }

    /// <summary>
    /// Enqueue a key by ConsoleKey (no character, no modifiers).
    /// </summary>
    public void EnqueueKey(ConsoleKey key)
    {
        EnqueueKey(new ConsoleKeyInfo('\0', key, shift: false, alt: false, control: false));
    }

    /// <summary>
    /// Enqueue a key by ConsoleKey with modifiers.
    /// </summary>
    public void EnqueueKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        char keyChar = key >= ConsoleKey.A && key <= ConsoleKey.Z
            ? (char)('a' + (key - ConsoleKey.A))
            : '\0';
        EnqueueKey(new ConsoleKeyInfo(keyChar, key, shift, alt, control));
    }

    /// <summary>
    /// Enqueue a character key.
    /// </summary>
    public void EnqueueChar(char c)
    {
        var consoleKey = char.ToUpper(c) switch
        {
            >= 'A' and <= 'Z' => (ConsoleKey)(char.ToUpper(c) - 'A' + (int)ConsoleKey.A),
            >= '0' and <= '9' => (ConsoleKey)(c - '0' + (int)ConsoleKey.D0),
            ' ' => ConsoleKey.Spacebar,
            _ => ConsoleKey.NoName
        };
        EnqueueKey(new ConsoleKeyInfo(c, consoleKey, shift: false, alt: false, control: false));
    }

    /// <summary>
    /// Enqueue a string as a series of character keys.
    /// </summary>
    public void EnqueueString(string text)
    {
        foreach (var c in text)
            EnqueueChar(c);
    }

    /// <summary>
    /// Enqueue a mouse event.
    /// </summary>
    /// <param name="x">X position (column).</param>
    /// <param name="y">Y position (row).</param>
    /// <param name="button">The mouse button.</param>
    /// <param name="eventType">The type of mouse event.</param>
    /// <param name="modifiers">Optional keyboard modifiers.</param>
    public void EnqueueMouse(int x, int y, MouseButton button, MouseEventType eventType, ConsoleModifiers modifiers = 0)
    {
        _inputChannel.Writer.TryWrite(new MouseEvent(x, y, button, eventType, modifiers));
    }

    /// <summary>
    /// Enqueue a mouse click event.
    /// </summary>
    public void EnqueueClick(int x, int y, MouseButton button = MouseButton.Left)
    {
        EnqueueMouse(x, y, button, MouseEventType.Press);
    }

    /// <summary>
    /// Enqueue a mouse scroll event.
    /// </summary>
    public void EnqueueScroll(int x, int y, bool up)
    {
        EnqueueMouse(x, y, up ? MouseButton.WheelUp : MouseButton.WheelDown, MouseEventType.Scroll);
    }

    /// <summary>
    /// Enqueue a terminal resize event.
    /// </summary>
    /// <param name="width">New terminal width.</param>
    /// <param name="height">New terminal height.</param>
    public void EnqueueResize(int width, int height)
    {
        _inputChannel.Writer.TryWrite(new ResizeEvent(width, height));
    }

    /// <summary>
    /// Signal that no more input will be provided.
    /// </summary>
    public void Complete()
    {
        _inputChannel.Writer.TryComplete();
    }

    /// <inheritdoc />
    public async Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        // Forward all enqueued input to the shared event channel
        await foreach (var evt in _inputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await writer.WriteAsync(evt, cancellationToken);
        }
    }
}

using System.Threading.Channels;

namespace Termina.Input;

/// <summary>
/// Virtual input source for testing and programmatic input.
/// External code pushes events via Enqueue methods, which get forwarded to the event loop.
/// </summary>
public sealed class VirtualInputSource : IInputSource
{
    private readonly Channel<KeyPressed> _inputChannel = Channel.CreateUnbounded<KeyPressed>();

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
        await foreach (var key in _inputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await writer.WriteAsync(key, cancellationToken);
        }
    }
}

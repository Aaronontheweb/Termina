namespace Termina;

/// <summary>
/// Abstraction for keyboard input - allows virtualizing input for testing
/// </summary>
public interface IInputSource
{
    /// <summary>
    /// Check if a key is available to read (non-blocking)
    /// </summary>
    bool IsKeyAvailable { get; }

    /// <summary>
    /// Read the next key press
    /// </summary>
    ConsoleKeyInfo ReadKey();
}

/// <summary>
/// Real console input implementation
/// </summary>
public sealed class ConsoleInputSource : IInputSource
{
    public bool IsKeyAvailable => Console.KeyAvailable;

    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);
}

/// <summary>
/// Virtualized input for testing - queues key presses that can be consumed
/// </summary>
public sealed class VirtualInputSource : IInputSource
{
    private readonly Queue<ConsoleKeyInfo> _keyQueue = new();

    public bool IsKeyAvailable => _keyQueue.Count > 0;

    public ConsoleKeyInfo ReadKey()
    {
        if (_keyQueue.Count == 0)
            throw new InvalidOperationException("No keys available in virtual input queue");

        return _keyQueue.Dequeue();
    }

    /// <summary>
    /// Queue a key press for consumption
    /// </summary>
    public void QueueKey(ConsoleKey key, char keyChar = '\0', bool shift = false, bool alt = false, bool control = false)
    {
        _keyQueue.Enqueue(new ConsoleKeyInfo(keyChar, key, shift, alt, control));
    }

    /// <summary>
    /// Queue a character key press
    /// </summary>
    public void QueueChar(char c)
    {
        var shift = char.IsUpper(c);
        _keyQueue.Enqueue(new ConsoleKeyInfo(c, (ConsoleKey)char.ToUpper(c), shift, false, false));
    }

    /// <summary>
    /// Queue a string of character key presses
    /// </summary>
    public void QueueString(string text)
    {
        foreach (var c in text)
        {
            QueueChar(c);
        }
    }
}

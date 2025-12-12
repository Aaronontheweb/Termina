namespace Termina.Components.Streaming;

/// <summary>
/// Defines how a streaming text buffer retains content.
/// </summary>
public enum StreamMode
{
    /// <summary>
    /// Full history is retained and scrollable.
    /// Suitable for chat messages, command output, logs that need review.
    /// </summary>
    Persisted,

    /// <summary>
    /// Only the last N lines are retained (rolling window).
    /// Suitable for ephemeral content like "thinking" indicators, progress updates.
    /// </summary>
    Windowed
}

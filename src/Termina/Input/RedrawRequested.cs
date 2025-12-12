namespace Termina.Input;

/// <summary>
/// Event requesting a UI redraw. Used by components that update asynchronously
/// (e.g., StreamingText consuming an IAsyncEnumerable) to signal that the
/// display needs to be refreshed.
/// </summary>
public sealed class RedrawRequested : IInputEvent
{
    /// <summary>
    /// Singleton instance to avoid allocations.
    /// </summary>
    public static readonly RedrawRequested Instance = new();

    private RedrawRequested() { }
}

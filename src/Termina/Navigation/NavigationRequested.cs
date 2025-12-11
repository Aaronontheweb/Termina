namespace Termina.Navigation;

/// <summary>
/// Event emitted to request navigation to a different page.
/// Handlers can emit this to trigger navigation through the event loop.
/// </summary>
public sealed record NavigationRequested(string PageKey) : IUIEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event emitted to request going back to the previous page.
/// </summary>
public sealed record NavigationBackRequested() : IUIEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

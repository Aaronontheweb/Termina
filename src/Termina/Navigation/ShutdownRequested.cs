namespace Termina.Navigation;

/// <summary>
/// Event emitted to request application shutdown.
/// The EventMediator will handle this and trigger graceful shutdown.
/// </summary>
public sealed record ShutdownRequested() : IUIEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

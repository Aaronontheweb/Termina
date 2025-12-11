using Termina.Events;

namespace Termina.Spike.Pages;

/// <summary>
/// UI events for the about page.
/// </summary>
public abstract record AboutUIEvent : IPageUIEvent
{
    /// <summary>
    /// User wants to go back to the previous page.
    /// </summary>
    public sealed record BackRequested() : AboutUIEvent;
}

/// <summary>
/// Commands sent from handler to about page.
/// </summary>
public abstract record AboutCommand : IUICommand
{
    // About page is static, no commands needed
}

namespace Termina.Events;

/// <summary>
/// Marker interface for page-specific UI events (user interactions).
/// These are strongly-typed domain events transformed from raw input.
/// </summary>
/// <remarks>
/// UI events flow: Raw Input → Page.MapToUIEvent() → Handler.HandleFrontend()
///
/// Each page defines its own UI event types, typically as a discriminated union:
/// <code>
/// public abstract record ChatUIEvent : IPageUIEvent
/// {
///     public sealed record MessageSubmitted(string Text) : ChatUIEvent;
///     public sealed record CancelRequested() : ChatUIEvent;
/// }
/// </code>
///
/// The page's MapToUIEvent method transforms raw input (KeyPressed, etc.)
/// into these typed domain events. Return null to ignore/swallow events.
/// </remarks>
public interface IPageUIEvent;

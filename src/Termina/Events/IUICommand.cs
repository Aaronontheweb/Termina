namespace Termina.Events;

/// <summary>
/// Marker interface for UI commands sent from handlers to pages.
/// Commands update page/component state and trigger re-renders.
/// </summary>
/// <remarks>
/// Commands flow: Handler → Page → Components
///
/// Each page defines its own command types, typically as a discriminated union:
/// <code>
/// public abstract record ChatCommand : IUICommand
/// {
///     public sealed record ShowMessage(string Role, string Text) : ChatCommand;
///     public sealed record ClearInput() : ChatCommand;
/// }
/// </code>
/// </remarks>
public interface IUICommand;

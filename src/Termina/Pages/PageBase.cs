using Spectre.Console.Rendering;
using Termina.Events;
using Termina.Input;

namespace Termina.Pages;

/// <summary>
/// Base class for pages in the Termina two-tier event architecture.
/// Pages contain components and transform raw input into typed UI events.
/// </summary>
/// <typeparam name="TUIEvent">The typed UI event type for this page (user interactions).</typeparam>
/// <typeparam name="TCommand">The command type that updates this page's state.</typeparam>
/// <remarks>
/// <para>
/// A page is the view layer in the architecture. It owns components and is responsible for:
/// </para>
/// <list type="bullet">
///   <item>Transforming raw input (KeyPressed) into typed domain UI events</item>
///   <item>Applying commands from the handler to update component state</item>
///   <item>Rendering components to the terminal</item>
/// </list>
/// <para>
/// Pages do NOT contain business logic - that belongs in the handler.
/// </para>
/// </remarks>
public abstract class PageBase<TUIEvent, TCommand> : IPage
    where TUIEvent : IPageUIEvent
    where TCommand : IUICommand
{
    /// <summary>
    /// Get all components on this page for rendering.
    /// </summary>
    public abstract IEnumerable<Component> Components { get; }

    /// <summary>
    /// Transform raw input into a typed UI event.
    /// </summary>
    /// <param name="raw">The raw input event (e.g., KeyPressed).</param>
    /// <returns>A typed UI event, or null to ignore the input.</returns>
    /// <remarks>
    /// <para>
    /// This method forces developers to think about what each input MEANS
    /// in the context of this page. Instead of handling raw keys everywhere,
    /// you define semantic events like "MessageSubmitted" or "CancelRequested".
    /// </para>
    /// <para>
    /// Return null to swallow/ignore events that aren't relevant.
    /// </para>
    /// <example>
    /// <code>
    /// protected override ChatUIEvent? MapToUIEvent(IInputEvent raw)
    /// {
    ///     return raw switch
    ///     {
    ///         KeyPressed { KeyInfo.Key: ConsoleKey.Enter } when Input.HasText
    ///             => new ChatUIEvent.MessageSubmitted(Input.Text),
    ///         KeyPressed { KeyInfo.Key: ConsoleKey.Escape }
    ///             => new ChatUIEvent.CancelRequested(),
    ///         _ => null
    ///     };
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected abstract TUIEvent? MapToUIEvent(IInputEvent raw);

    /// <summary>
    /// Apply a command to update page/component state.
    /// </summary>
    /// <param name="command">The command from the handler.</param>
    /// <remarks>
    /// <para>
    /// Commands are how the handler updates the UI. The handler calls Send()
    /// which invokes this method. Implement a switch over command types.
    /// </para>
    /// <example>
    /// <code>
    /// protected override void ApplyCommand(ChatCommand command)
    /// {
    ///     switch (command)
    ///     {
    ///         case ChatCommand.ShowMessage(var role, var text):
    ///             Messages.Add(role, text);
    ///             break;
    ///         case ChatCommand.ClearInput():
    ///             Input.Clear();
    ///             break;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected abstract void ApplyCommand(TCommand command);

    /// <summary>
    /// Render the page as a Spectre.Console renderable.
    /// </summary>
    public abstract IRenderable Render();

    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Override to initialize or refresh component state.
    /// </summary>
    public virtual void OnNavigatedTo() { }

    /// <summary>
    /// Called when the page is about to become inactive (navigating away).
    /// Override to save state or clean up.
    /// </summary>
    public virtual void OnNavigatingFrom() { }

    /// <summary>
    /// Internal method called by the framework to transform input.
    /// </summary>
    internal TUIEvent? TransformInput(IInputEvent raw) => MapToUIEvent(raw);

    /// <summary>
    /// Internal method called by the framework to apply commands.
    /// </summary>
    internal void ReceiveCommand(TCommand command) => ApplyCommand(command);
}

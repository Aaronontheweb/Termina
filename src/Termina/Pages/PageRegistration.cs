using Termina.Input;

namespace Termina.Pages;

/// <summary>
/// Registration info for a page.
/// Contains factories and delegates for creating and wiring up page/handler pairs.
/// All type information is captured in the delegate closures at compile time.
/// </summary>
public sealed record PageRegistration
{
    /// <summary>
    /// Factory to create the page instance.
    /// </summary>
    public required Func<IPage> PageFactory { get; init; }

    /// <summary>
    /// Factory to create the handler instance.
    /// </summary>
    public required Func<object> HandlerFactory { get; init; }

    /// <summary>
    /// How the page behaves on navigation (reset vs preserve state).
    /// </summary>
    public required NavigationBehavior Behavior { get; init; }

    /// <summary>
    /// Wires up the handler to the page and navigation actions.
    /// </summary>
    public required Action<object, IPage, Action<string>, Action> WireUpHandler { get; init; }

    /// <summary>
    /// Invokes HandleUIEvent on the handler with a UI event.
    /// </summary>
    public required Action<object, object> InvokeHandleUIEvent { get; init; }

    /// <summary>
    /// Invokes OnNavigatedTo on the handler.
    /// </summary>
    public required Action<object> InvokeOnNavigatedTo { get; init; }

    /// <summary>
    /// Invokes OnNavigatingFrom on the handler.
    /// </summary>
    public required Action<object> InvokeOnNavigatingFrom { get; init; }

    /// <summary>
    /// Transforms raw input using the page's MapToUIEvent method.
    /// Returns null if the event should be ignored.
    /// </summary>
    public required Func<IPage, IInputEvent, object?> TransformInput { get; init; }
}

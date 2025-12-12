using Spectre.Console.Rendering;

namespace Termina;

/// <summary>
/// Base class for all UI components in Termina.
/// Components are simple, reusable building blocks that render Spectre.Console widgets.
/// </summary>
/// <remarks>
/// <para>
/// Components in the reactive architecture are simpler than before:
/// </para>
/// <list type="bullet">
///   <item>They render UI based on their current state</item>
///   <item>State is updated by Pages that subscribe to ViewModel observables</item>
///   <item>Components don't own state - they just render it</item>
/// </list>
/// <para>
/// For complex components that need reactive state, extend ReactiveComponent instead.
/// </para>
/// </remarks>
public abstract class Component
{
    /// <summary>
    /// Render this component as a Spectre.Console renderable.
    /// Called after state changes to update the display.
    /// </summary>
    public abstract IRenderable Render();
}

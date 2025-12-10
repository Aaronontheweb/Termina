namespace Termina;

/// <summary>
/// Base interface for all UI components
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Render this component to lines of text
    /// </summary>
    /// <param name="context">Rendering context with available space and focus information</param>
    /// <returns>Array of lines representing the rendered component</returns>
    string[] Render(RenderContext context);

    /// <summary>
    /// Measure the size this component would need
    /// </summary>
    Size MeasureSize(int availableWidth, int availableHeight);

    /// <summary>
    /// Whether this component can receive focus (for interactive components)
    /// </summary>
    bool CanFocus { get; }

    /// <summary>
    /// Handle a key press (for interactive components)
    /// </summary>
    void OnKeyPress(ConsoleKeyInfo key);

    /// <summary>
    /// Child components (for layout components)
    /// </summary>
    IReadOnlyList<IComponent> Children { get; }
}

/// <summary>
/// Rendering context passed to components
/// </summary>
public record RenderContext(
    int Width,
    int Height,
    IComponent? FocusedComponent = null);

/// <summary>
/// Component size
/// </summary>
public record Size(int Width, int Height);

/// <summary>
/// Base class for components
/// </summary>
public abstract class Component : IComponent
{
    private readonly List<IComponent> _children = new();

    public IReadOnlyList<IComponent> Children => _children;

    public virtual bool CanFocus => false;

    public abstract string[] Render(RenderContext context);

    public abstract Size MeasureSize(int availableWidth, int availableHeight);

    public virtual void OnKeyPress(ConsoleKeyInfo key) { }

    protected void AddChild(IComponent child) => _children.Add(child);
}

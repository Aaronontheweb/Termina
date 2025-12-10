using Termina.Components;

namespace Termina;

/// <summary>
/// Static factory methods for creating components with concise syntax
/// </summary>
public static class UI
{
    /// <summary>
    /// Create a text component
    /// </summary>
    public static Text Text(string content) => new Text(content);

    /// <summary>
    /// Create a panel component
    /// </summary>
    public static Panel Panel(string? header = null) => new Panel(header);

    /// <summary>
    /// Create a text input component
    /// </summary>
    public static TextInput TextInput() => new TextInput();

    /// <summary>
    /// Create a rows layout component
    /// </summary>
    public static Rows Rows() => new Rows();
}

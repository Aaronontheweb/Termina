namespace Termina;

/// <summary>
/// Specifies how a page should be rendered.
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// Uses Spectre.Console's LiveDisplay to render in place.
    /// The screen is cleared and redrawn each frame.
    /// Best for: settings pages, menus, dashboards, forms.
    /// </summary>
    LiveDisplay,

    /// <summary>
    /// Writes content directly to the console with natural scrolling.
    /// Content scrolls up as more is added, using terminal's native scrollback.
    /// Best for: chat interfaces, logs, command output, streaming content.
    /// </summary>
    Scrolling
}

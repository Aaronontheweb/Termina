namespace Termina.Pages;

/// <summary>
/// Marker interface for page handlers.
/// </summary>
/// <remarks>
/// <para>
/// Handlers receive UI events from pages and send commands to update them.
/// Use constructor injection to get any services the handler needs.
/// </para>
/// <para>
/// All concrete handlers must inherit from <see cref="PageHandler{TPage, TUIEvent, TCommand}"/>
/// which provides the <c>CreateRegistration</c> static method used by the framework.
/// </para>
/// </remarks>
public interface IPageHandler
{
    /// <summary>
    /// Creates a registration for this handler type.
    /// Implemented by <see cref="PageHandler{TPage, TUIEvent, TCommand}"/>.
    /// Called via reflection by the framework.
    /// </summary>
    /// <param name="pageKey">The unique key for this page.</param>
    /// <param name="behavior">Navigation behavior for this page.</param>
    /// <returns>A page registration with typed delegate closures.</returns>
    static abstract PageRegistration CreateRegistration(
        string pageKey,
        NavigationBehavior behavior);
}

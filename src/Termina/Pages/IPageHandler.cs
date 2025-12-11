using Termina.Events;

namespace Termina.Pages;

/// <summary>
/// Interface for page handlers that provides static type information.
/// Used for AOT-compatible registration without reflection.
/// </summary>
/// <remarks>
/// This interface uses static abstract members (C# 11 / .NET 7+) to expose
/// type metadata at compile time, avoiding runtime reflection.
/// </remarks>
public interface IPageHandler<THandler> where THandler : new()
{
    /// <summary>
    /// Gets the page type this handler controls.
    /// </summary>
    static abstract Type PageType { get; }

    /// <summary>
    /// Gets the UI event type for this handler's page.
    /// </summary>
    static abstract Type UIEventType { get; }

    /// <summary>
    /// Gets the command type for this handler's page.
    /// </summary>
    static abstract Type CommandType { get; }

    /// <summary>
    /// Creates a registration for this handler type.
    /// This method enables AOT-compatible registration without reflection.
    /// </summary>
    /// <param name="pageKey">The unique key for this page.</param>
    /// <param name="behavior">Navigation behavior for this page.</param>
    /// <returns>A page registration that can be added to the application.</returns>
    static abstract PageRegistration CreateRegistration(
        string pageKey,
        NavigationBehavior behavior);
}

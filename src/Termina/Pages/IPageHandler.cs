namespace Termina.Pages;

/// <summary>
/// Interface for page handlers that provides AOT-compatible registration.
/// </summary>
/// <remarks>
/// This interface uses static abstract members (C# 11 / .NET 7+) to create
/// typed registrations at compile time without reflection.
/// </remarks>
public interface IPageHandler
{
    /// <summary>
    /// Creates a registration for this handler type.
    /// The concrete handler implements this to create typed delegates
    /// that capture all type information at compile time.
    /// </summary>
    /// <param name="pageKey">The unique key for this page.</param>
    /// <param name="behavior">Navigation behavior for this page.</param>
    /// <returns>A page registration with typed delegate closures.</returns>
    static abstract PageRegistration CreateRegistration(
        string pageKey,
        NavigationBehavior behavior);
}

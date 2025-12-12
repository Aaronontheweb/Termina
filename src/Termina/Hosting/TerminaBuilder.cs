using Microsoft.Extensions.DependencyInjection;
using Termina.Navigation;
using Termina.Pages;

namespace Termina.Hosting;

/// <summary>
/// Builder for configuring Termina page registrations.
/// </summary>
public sealed class TerminaBuilder
{
    private readonly IServiceCollection _services;
    internal readonly List<PageRegistrationDescriptor> PageDescriptors = new();

    internal TerminaBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Register a page with its handler.
    /// The handler will be resolved from DI, allowing constructor injection.
    /// </summary>
    /// <typeparam name="THandler">The handler type (must implement IPageHandler).</typeparam>
    /// <param name="pageKey">Unique key to identify this page for navigation.</param>
    /// <param name="behavior">How the page behaves on navigation (default: ResetOnNavigation).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public TerminaBuilder RegisterPage<THandler>(
        string pageKey,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where THandler : class, IPageHandler
    {
        // Register handler with DI
        _services.AddTransient<THandler>();

        // Store descriptor for later registration with TerminaApplication
        PageDescriptors.Add(new PageRegistrationDescriptor(
            pageKey,
            typeof(THandler),
            behavior,
            sp => sp.GetRequiredService<THandler>()));

        return this;
    }

    /// <summary>
    /// Register a page with a custom handler factory.
    /// Use this when you need custom initialization beyond DI.
    /// </summary>
    /// <typeparam name="THandler">The handler type (must implement IPageHandler).</typeparam>
    /// <param name="pageKey">Unique key to identify this page for navigation.</param>
    /// <param name="handlerFactory">Factory to create the handler instance.</param>
    /// <param name="behavior">How the page behaves on navigation (default: ResetOnNavigation).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public TerminaBuilder RegisterPage<THandler>(
        string pageKey,
        Func<IServiceProvider, THandler> handlerFactory,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where THandler : class, IPageHandler
    {
        PageDescriptors.Add(new PageRegistrationDescriptor(
            pageKey,
            typeof(THandler),
            behavior,
            sp => handlerFactory(sp)));

        return this;
    }
}

/// <summary>
/// Descriptor for a page registration, used to defer registration until the application starts.
/// </summary>
internal sealed record PageRegistrationDescriptor(
    string PageKey,
    Type HandlerType,
    NavigationBehavior Behavior,
    Func<IServiceProvider, object> HandlerFactory);

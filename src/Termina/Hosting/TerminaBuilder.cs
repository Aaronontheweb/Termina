using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Termina.Pages;
using Termina.Reactive;
using Termina.Routing;

namespace Termina.Hosting;

/// <summary>
/// Builder for configuring Termina page registrations.
/// </summary>
public sealed class TerminaBuilder
{
    private readonly IServiceCollection _services;
    internal readonly List<ReactivePageRegistrationDescriptor> PageDescriptors = new();
    internal TerminaRuntimeOptions RuntimeOptions { get; } = new();

    internal TerminaBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Register a reactive page with a route template.
    /// Both the Page and ViewModel will be resolved from DI, allowing constructor injection.
    /// </summary>
    /// <typeparam name="TPage">The page type (must extend ReactivePage&lt;TViewModel&gt;).</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="routeTemplate">Route template (e.g., "/tasks/{id:int}").</param>
    /// <param name="behavior">How the page behaves on navigation (default: ResetOnNavigation).</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// builder.RegisterRoute&lt;TodoListPage, TodoListViewModel&gt;("/todos")
    ///        .RegisterRoute&lt;TodoDetailPage, TodoDetailViewModel&gt;("/todos/{id:int}");
    /// </code>
    /// </example>
    public TerminaBuilder RegisterRoute<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        string routeTemplate,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : ReactivePage<TViewModel>
        where TViewModel : ReactiveViewModel
    {
        var parsedTemplate = RouteParser.Parse(routeTemplate);

        // Register Page and ViewModel with DI
        _services.AddTransient<TPage>();
        _services.AddTransient<TViewModel>();

        // Store descriptor for later registration with TerminaApplication
        PageDescriptors.Add(new ReactivePageRegistrationDescriptor(
            parsedTemplate,
            typeof(TPage),
            typeof(TViewModel),
            behavior,
            sp => sp.GetRequiredService<TPage>(),
            sp => sp.GetRequiredService<TViewModel>()));

        return this;
    }

    /// <summary>
    /// Register a page with a route template and custom factories for Page and ViewModel.
    /// Use this when you need custom initialization beyond DI.
    /// </summary>
    /// <typeparam name="TPage">The page type (must extend ReactivePage&lt;TViewModel&gt;).</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="routeTemplate">Route template (e.g., "/tasks/{id:int}").</param>
    /// <param name="pageFactory">Factory to create the Page instance.</param>
    /// <param name="viewModelFactory">Factory to create the ViewModel instance.</param>
    /// <param name="behavior">How the page behaves on navigation (default: ResetOnNavigation).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public TerminaBuilder RegisterRoute<TPage, TViewModel>(
        string routeTemplate,
        Func<IServiceProvider, TPage> pageFactory,
        Func<IServiceProvider, TViewModel> viewModelFactory,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : ReactivePage<TViewModel>
        where TViewModel : ReactiveViewModel
    {
        var parsedTemplate = RouteParser.Parse(routeTemplate);

        PageDescriptors.Add(new ReactivePageRegistrationDescriptor(
            parsedTemplate,
            typeof(TPage),
            typeof(TViewModel),
            behavior,
            sp => pageFactory(sp),
            sp => viewModelFactory(sp)));

        return this;
    }

    /// <summary>
    /// Configure runtime terminal/input behavior for the hosted Termina application.
    /// </summary>
    public TerminaBuilder ConfigureRuntime(Action<TerminaRuntimeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(RuntimeOptions);
        return this;
    }
}

/// <summary>
/// Descriptor for a reactive page registration, used to defer registration until the application starts.
/// </summary>
internal sealed class ReactivePageRegistrationDescriptor
{
    /// <summary>
    /// The parsed route template for this page.
    /// </summary>
    public RouteTemplate RouteTemplate { get; }

    /// <summary>
    /// A unique key derived from the route template.
    /// Used internally for page caching.
    /// </summary>
    public string PageKey => RouteTemplate.Template;

    public Type PageType { get; }
    public Type ViewModelType { get; }
    public NavigationBehavior Behavior { get; }
    public Func<IServiceProvider, object> PageFactory { get; }
    public Func<IServiceProvider, ReactiveViewModel> ViewModelFactory { get; }

    public ReactivePageRegistrationDescriptor(
        RouteTemplate routeTemplate,
        Type pageType,
        Type viewModelType,
        NavigationBehavior behavior,
        Func<IServiceProvider, object> pageFactory,
        Func<IServiceProvider, ReactiveViewModel> viewModelFactory)
    {
        RouteTemplate = routeTemplate;
        PageType = pageType;
        ViewModelType = viewModelType;
        Behavior = behavior;
        PageFactory = pageFactory;
        ViewModelFactory = viewModelFactory;
    }
}

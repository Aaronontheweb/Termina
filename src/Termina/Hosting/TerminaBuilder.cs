using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Termina.Pages;
using Termina.Reactive;

namespace Termina.Hosting;

/// <summary>
/// Builder for configuring Termina page registrations.
/// </summary>
public sealed class TerminaBuilder
{
    private readonly IServiceCollection _services;
    internal readonly List<ReactivePageRegistrationDescriptor> PageDescriptors = new();

    internal TerminaBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Register a reactive page with its ViewModel.
    /// Both the Page and ViewModel will be resolved from DI, allowing constructor injection.
    /// </summary>
    /// <typeparam name="TPage">The page type (must extend ReactivePage&lt;TViewModel&gt;).</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="pageKey">Unique key to identify this page for navigation.</param>
    /// <param name="behavior">How the page behaves on navigation (default: ResetOnNavigation).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public TerminaBuilder RegisterPage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        string pageKey,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : ReactivePage<TViewModel>
        where TViewModel : ReactiveViewModel
    {
        // Register Page and ViewModel with DI
        _services.AddTransient<TPage>();
        _services.AddTransient<TViewModel>();

        // Store descriptor for later registration with TerminaApplication
        PageDescriptors.Add(new ReactivePageRegistrationDescriptor(
            pageKey,
            typeof(TPage),
            typeof(TViewModel),
            behavior,
            sp => sp.GetRequiredService<TPage>(),
            sp => sp.GetRequiredService<TViewModel>()));

        return this;
    }

    /// <summary>
    /// Register a page with custom factories for Page and ViewModel.
    /// Use this when you need custom initialization beyond DI.
    /// </summary>
    /// <typeparam name="TPage">The page type (must extend ReactivePage&lt;TViewModel&gt;).</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="pageKey">Unique key to identify this page for navigation.</param>
    /// <param name="pageFactory">Factory to create the Page instance.</param>
    /// <param name="viewModelFactory">Factory to create the ViewModel instance.</param>
    /// <param name="behavior">How the page behaves on navigation (default: ResetOnNavigation).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public TerminaBuilder RegisterPage<TPage, TViewModel>(
        string pageKey,
        Func<IServiceProvider, TPage> pageFactory,
        Func<IServiceProvider, TViewModel> viewModelFactory,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : ReactivePage<TViewModel>
        where TViewModel : ReactiveViewModel
    {
        PageDescriptors.Add(new ReactivePageRegistrationDescriptor(
            pageKey,
            typeof(TPage),
            typeof(TViewModel),
            behavior,
            sp => pageFactory(sp),
            sp => viewModelFactory(sp)));

        return this;
    }
}

/// <summary>
/// Descriptor for a reactive page registration, used to defer registration until the application starts.
/// </summary>
internal sealed class ReactivePageRegistrationDescriptor
{
    public string PageKey { get; }
    public Type PageType { get; }
    public Type ViewModelType { get; }
    public NavigationBehavior Behavior { get; }
    public Func<IServiceProvider, object> PageFactory { get; }
    public Func<IServiceProvider, ReactiveViewModel> ViewModelFactory { get; }

    public ReactivePageRegistrationDescriptor(
        string pageKey,
        Type pageType,
        Type viewModelType,
        NavigationBehavior behavior,
        Func<IServiceProvider, object> pageFactory,
        Func<IServiceProvider, ReactiveViewModel> viewModelFactory)
    {
        PageKey = pageKey;
        PageType = pageType;
        ViewModelType = viewModelType;
        Behavior = behavior;
        PageFactory = pageFactory;
        ViewModelFactory = viewModelFactory;
    }
}

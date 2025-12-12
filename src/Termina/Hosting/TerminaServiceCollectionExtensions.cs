using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console;
using Termina.Input;

namespace Termina.Hosting;

/// <summary>
/// Extension methods for registering Termina with dependency injection.
/// </summary>
public static class TerminaServiceCollectionExtensions
{
    /// <summary>
    /// Adds Termina TUI framework services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure page registrations.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddTermina(termina =>
    /// {
    ///     termina.RegisterPage&lt;MainMenuHandler&gt;("main-menu");
    ///     termina.RegisterPage&lt;SettingsHandler&gt;("settings");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTermina(
        this IServiceCollection services,
        Action<TerminaBuilder> configure)
    {
        return AddTermina(services, startPage: null, configure);
    }

    /// <summary>
    /// Adds Termina TUI framework services to the service collection with a start page.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="startPage">The page key to navigate to on startup.</param>
    /// <param name="configure">Action to configure page registrations.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTermina(
        this IServiceCollection services,
        string? startPage,
        Action<TerminaBuilder> configure)
    {
        var builder = new TerminaBuilder(services);
        configure(builder);

        // Register IAnsiConsole if not already registered
        services.TryAddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Register TerminaApplication
        services.AddSingleton<TerminaApplication>(sp =>
        {
            var console = sp.GetRequiredService<IAnsiConsole>();
            var app = new TerminaApplication(console, sp);

            // Register all pages from the builder
            foreach (var descriptor in builder.PageDescriptors)
            {
                app.RegisterPageFromDescriptor(descriptor);
            }

            // Navigate to start page if specified
            if (!string.IsNullOrEmpty(startPage))
            {
                app.NavigateTo(startPage);
            }

            return app;
        });

        // Register hosted service to run Termina
        services.AddHostedService<TerminaHostedService>();

        return services;
    }

    /// <summary>
    /// Adds a console input source to Termina.
    /// This is the default if no other input source is configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTerminaConsoleInput(this IServiceCollection services)
    {
        services.AddSingleton<IInputSource, ConsoleInputSource>();
        return services;
    }

    /// <summary>
    /// Adds a virtual input source to Termina for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="inputSource">The virtual input source instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTerminaVirtualInput(
        this IServiceCollection services,
        VirtualInputSource inputSource)
    {
        services.AddSingleton<IInputSource>(inputSource);
        return services;
    }
}

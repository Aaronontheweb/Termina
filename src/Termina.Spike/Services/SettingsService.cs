using Termina.Events;
using Termina.Pages;

namespace Termina.Spike.Services;

/// <summary>
/// Model events for the settings service.
/// These flow between the Model layer and the UI.
/// </summary>
public abstract record SettingsModelEvent : IModelEvent
{
    /// <summary>
    /// Request to load current settings from storage.
    /// Published by UI, handled by SettingsService.
    /// </summary>
    public sealed record LoadSettings() : SettingsModelEvent;

    /// <summary>
    /// Request to save username.
    /// Published by UI, handled by SettingsService.
    /// </summary>
    public sealed record SaveUsername(string Username) : SettingsModelEvent;

    /// <summary>
    /// Request to save theme preference.
    /// Published by UI, handled by SettingsService.
    /// </summary>
    public sealed record SaveTheme(string Theme) : SettingsModelEvent;

    /// <summary>
    /// Settings have been loaded from storage.
    /// Published by SettingsService, handled by UI.
    /// </summary>
    public sealed record SettingsLoaded(string Username, string Theme) : SettingsModelEvent;

    /// <summary>
    /// Username was saved successfully.
    /// Published by SettingsService, handled by UI.
    /// </summary>
    public sealed record UsernameSaved(string Username) : SettingsModelEvent;

    /// <summary>
    /// Theme was saved successfully.
    /// Published by SettingsService, handled by UI.
    /// </summary>
    public sealed record ThemeSaved(string Theme) : SettingsModelEvent;

    /// <summary>
    /// An error occurred while saving/loading settings.
    /// Published by SettingsService, handled by UI.
    /// </summary>
    public sealed record SettingsError(string Message) : SettingsModelEvent;
}

/// <summary>
/// Backend service that manages application settings.
/// This simulates async storage operations (like a database or file system).
/// </summary>
/// <remarks>
/// <para>
/// In a real application, this could be:
/// - An Akka.NET actor that persists to event store
/// - A service that writes to a JSON file
/// - A service that calls a remote API
/// </para>
/// <para>
/// The service subscribes to events from the UI and publishes events
/// back when operations complete. The UI never needs to know the
/// implementation details.
/// </para>
/// </remarks>
public sealed class SettingsService : IDisposable
{
    private readonly IApplicationBus _bus;
    private readonly List<IDisposable> _subscriptions = new();

    // Simulated storage
    private string _username = "";
    private string _theme = "System Default";

    // Simulate async delay for operations
    private readonly int _simulatedDelayMs;

    public SettingsService(IApplicationBus bus, int simulatedDelayMs = 500)
    {
        _bus = bus;
        _simulatedDelayMs = simulatedDelayMs;

        // Subscribe to events from UI
        _subscriptions.Add(bus.Subscribe<SettingsModelEvent.LoadSettings>(OnLoadSettings));
        _subscriptions.Add(bus.Subscribe<SettingsModelEvent.SaveUsername>(OnSaveUsername));
        _subscriptions.Add(bus.Subscribe<SettingsModelEvent.SaveTheme>(OnSaveTheme));
    }

    private void OnLoadSettings(SettingsModelEvent.LoadSettings evt)
    {
        // Simulate async load from storage
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_simulatedDelayMs);

                // Publish loaded settings back to UI
                _bus.Publish(new SettingsModelEvent.SettingsLoaded(_username, _theme));
            }
            catch (Exception ex)
            {
                _bus.Publish(new SettingsModelEvent.SettingsError($"Failed to load settings: {ex.Message}"));
            }
        });
    }

    private void OnSaveUsername(SettingsModelEvent.SaveUsername evt)
    {
        // Simulate async save to storage
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_simulatedDelayMs);

                // Update stored value
                _username = evt.Username;

                // Publish success back to UI
                _bus.Publish(new SettingsModelEvent.UsernameSaved(evt.Username));
            }
            catch (Exception ex)
            {
                _bus.Publish(new SettingsModelEvent.SettingsError($"Failed to save username: {ex.Message}"));
            }
        });
    }

    private void OnSaveTheme(SettingsModelEvent.SaveTheme evt)
    {
        // Simulate async save to storage
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_simulatedDelayMs);

                // Update stored value
                _theme = evt.Theme;

                // Publish success back to UI
                _bus.Publish(new SettingsModelEvent.ThemeSaved(evt.Theme));
            }
            catch (Exception ex)
            {
                _bus.Publish(new SettingsModelEvent.SettingsError($"Failed to save theme: {ex.Message}"));
            }
        });
    }

    public void Dispose()
    {
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
    }
}

using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Components;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Settings page demonstrating TextInput and SelectList components.
/// Configured with PreserveState to show that form data persists across navigation.
/// </summary>
public sealed class SettingsPage : IPage
{
    private readonly TextInput _username;
    private readonly SelectList _theme;
    private bool _initialized = false;

    public SettingsPage()
    {
        _username = new TextInput("Username", "Enter your username...");
        _theme = new SelectList("Theme");
    }

    public IEnumerable<Component> Components => [_username, _theme];

    public void OnNavigatedTo()
    {
        // Only initialize options on first visit
        // State persists because this page uses PreserveState
        if (!_initialized)
        {
            _theme.ReceiveEvent(new SetOptions(["Light", "Dark", "System Default"]));
            _initialized = true;
        }
    }

    public void OnNavigatingFrom()
    {
        // State is preserved - nothing to do
    }

    public IRenderable Render()
    {
        return new Rows(
            new Rule("[bold]Settings[/]").RuleStyle("aqua"),
            new Text(""),
            _username.Render(),
            new Text(""),
            _theme.Render(),
            new Text(""),
            new Markup("[grey]Type to enter username, arrows to select theme[/]"),
            new Markup("[grey]Press Escape to go back (your changes are preserved!)[/]")
        );
    }
}

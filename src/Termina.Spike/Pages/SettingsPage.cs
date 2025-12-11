using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Components;
using Termina.Input;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Settings page using the two-tier event architecture.
/// Demonstrates PreserveState behavior and backend integration.
/// </summary>
public sealed class SettingsPage : PageBase<SettingsUIEvent, SettingsCommand>
{
    private readonly TextInput _username;
    private readonly SelectList _theme;
    private readonly List<string> _themeOptions = new();
    private bool _focusOnUsername = true;
    private string _statusMessage = "";
    private bool _statusIsError = false;
    private string _loadingMessage = "";
    private bool _isLoading = false;

    public SettingsPage()
    {
        _username = new TextInput("", "Enter your username...", minWidth: 40);
        _theme = new SelectList("Theme");
    }

    public override IEnumerable<Component> Components => [_username, _theme];

    protected override SettingsUIEvent? MapToUIEvent(IInputEvent raw)
    {
        if (raw is not KeyPressed key) return null;

        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.Escape:
                return new SettingsUIEvent.BackRequested();

            case ConsoleKey.Tab:
                // Toggle focus between components
                _focusOnUsername = !_focusOnUsername;
                return null;

            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                // Only theme selector handles arrow keys
                if (!_focusOnUsername)
                {
                    _theme.HandleInput(key);
                }
                return null;

            case ConsoleKey.Enter:
                if (_focusOnUsername)
                {
                    // Submit username
                    var text = _username.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        return new SettingsUIEvent.UsernameSubmitted(text);
                    }
                }
                else
                {
                    // Select theme
                    var theme = _theme.HighlightedValue;
                    if (theme != null)
                    {
                        return new SettingsUIEvent.ThemeSelected(theme);
                    }
                }
                return null;

            default:
                // Text input handles character keys
                if (_focusOnUsername)
                {
                    _username.HandleInput(key);
                }
                return null;
        }
    }

    protected override void ApplyCommand(SettingsCommand command)
    {
        switch (command)
        {
            case SettingsCommand.InitializeThemes(var themes):
                _themeOptions.Clear();
                _themeOptions.AddRange(themes);
                _theme.ReceiveEvent(new SetOptions(themes));
                break;

            case SettingsCommand.SetUsername(var username):
                _username.ReceiveEvent(new SetText(username));
                break;

            case SettingsCommand.SelectTheme(var themeName):
                var index = _themeOptions.IndexOf(themeName);
                if (index >= 0)
                {
                    _theme.ReceiveEvent(new SelectOption(index));
                }
                break;

            case SettingsCommand.ShowStatus(var message, var isError):
                _statusMessage = message;
                _statusIsError = isError;
                break;

            case SettingsCommand.ShowLoading(var message):
                _loadingMessage = message;
                _isLoading = true;
                break;

            case SettingsCommand.HideLoading:
                _isLoading = false;
                _loadingMessage = "";
                break;
        }
    }

    public override IRenderable Render()
    {
        // Visual indication of which component has focus
        var usernameBorder = _focusOnUsername ? Color.Aqua : Color.Grey;
        var themeBorder = !_focusOnUsername ? Color.Aqua : Color.Grey;

        var rows = new List<IRenderable>
        {
            new Rule("[bold]Settings[/]").RuleStyle("aqua"),
            new Text("")
        };

        // Loading indicator
        if (_isLoading)
        {
            rows.Add(new Markup($"[yellow]⏳ {Markup.Escape(_loadingMessage)}[/]"));
            rows.Add(new Text(""));
        }

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var statusColor = _statusIsError ? "red" : "green";
            var statusIcon = _statusIsError ? "❌" : "✓";
            rows.Add(new Markup($"[{statusColor}]{statusIcon} {Markup.Escape(_statusMessage)}[/]"));
            rows.Add(new Text(""));
        }

        // Username input
        rows.Add(new Panel(_username.Render())
            .Border(BoxBorder.Rounded)
            .BorderColor(usernameBorder)
            .Header("[bold]Username[/]"));
        rows.Add(new Text(""));

        // Theme selector
        rows.Add(new Panel(_theme.Render())
            .Border(BoxBorder.Rounded)
            .BorderColor(themeBorder)
            .Header("[bold]Theme[/]"));
        rows.Add(new Text(""));

        // Help text
        rows.Add(new Markup("[grey]Tab to switch focus, Enter to save, Escape to go back[/]"));

        return new Rows(rows);
    }
}

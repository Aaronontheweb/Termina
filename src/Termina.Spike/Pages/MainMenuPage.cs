using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Components;
using Termina.Input;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Main menu page using the two-tier event architecture.
/// </summary>
public sealed class MainMenuPage : PageBase<MainMenuUIEvent, MainMenuCommand>
{
    private readonly SelectList _menu;

    public MainMenuPage()
    {
        _menu = new SelectList("Main Menu");
    }

    public override IEnumerable<Component> Components => [_menu];

    protected override MainMenuUIEvent? MapToUIEvent(IInputEvent raw)
    {
        // For navigation keys (up/down), let the component handle them internally
        // We return null to swallow these events at the handler level
        if (raw is KeyPressed key)
        {
            switch (key.KeyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    // Let component handle navigation internally
                    _menu.HandleInput(key);
                    return null;

                case ConsoleKey.Enter:
                    // Transform Enter into a semantic event
                    var selected = _menu.HighlightedValue;
                    if (selected != null)
                    {
                        return new MainMenuUIEvent.MenuItemSelected(selected);
                    }
                    return null;

                default:
                    // Ignore other keys
                    return null;
            }
        }

        return null;
    }

    protected override void ApplyCommand(MainMenuCommand command)
    {
        switch (command)
        {
            case MainMenuCommand.InitializeMenu(var options):
                _menu.ReceiveEvent(new SetOptions(options));
                break;
        }
    }

    public override void OnNavigatedTo()
    {
        // Menu initialization is handled by handler via commands
    }

    public override IRenderable Render()
    {
        return new Rows(
            new FigletText("Termina")
                .Color(Color.Aqua),
            new Text(""),
            _menu.Render(),
            new Text(""),
            new Markup("[grey]Use arrow keys to navigate, Enter to select[/]")
        );
    }
}

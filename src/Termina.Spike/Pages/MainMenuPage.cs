using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Components;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Main menu page demonstrating SelectList component.
/// </summary>
public sealed class MainMenuPage : IPage
{
    private readonly SelectList _menu;

    public MainMenuPage()
    {
        _menu = new SelectList("Main Menu");
    }

    public IEnumerable<Component> Components => [_menu];

    public void OnNavigatedTo()
    {
        // Set up menu options
        _menu.ReceiveEvent(new SetOptions(["Settings", "About", "Exit"]));
    }

    public void OnNavigatingFrom()
    {
        // Nothing to clean up
    }

    public IRenderable Render()
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

using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Simple about page demonstrating ResetOnNavigation behavior.
/// </summary>
public sealed class AboutPage : IPage
{
    public IEnumerable<Component> Components => [];

    public void OnNavigatedTo()
    {
        // Nothing to initialize
    }

    public void OnNavigatingFrom()
    {
        // Nothing to clean up
    }

    public IRenderable Render()
    {
        return new Rows(
            new Rule("[bold]About Termina[/]").RuleStyle("aqua"),
            new Text(""),
            new Panel(new Markup(
                "[bold]Termina[/] is a TUI framework built on [blue]Spectre.Console[/].\n\n" +
                "Features:\n" +
                "  [green]•[/] Two-tier event architecture\n" +
                "  [green]•[/] Page-based navigation\n" +
                "  [green]•[/] Reusable components\n" +
                "  [green]•[/] Single-threaded event loop\n\n" +
                "[grey]Press Escape to go back[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Aqua)
        );
    }
}

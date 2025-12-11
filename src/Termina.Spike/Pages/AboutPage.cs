using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Input;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// About page using the two-tier event architecture.
/// Simple static content page demonstrating ResetOnNavigation behavior.
/// </summary>
public sealed class AboutPage : PageBase<AboutUIEvent, AboutCommand>
{
    public override IEnumerable<Component> Components => [];

    protected override AboutUIEvent? MapToUIEvent(IInputEvent raw)
    {
        if (raw is KeyPressed key && key.KeyInfo.Key == ConsoleKey.Escape)
        {
            return new AboutUIEvent.BackRequested();
        }

        return null;
    }

    protected override void ApplyCommand(AboutCommand command)
    {
        // No commands to handle - static page
    }

    public override IRenderable Render()
    {
        return new Rows(
            new Rule("[bold]About Termina[/]").RuleStyle("aqua"),
            new Text(""),
            new Panel(new Markup(
                "[bold]Termina[/] is a TUI framework built on [blue]Spectre.Console[/].\n\n" +
                "Features:\n" +
                "  [green]•[/] Two-tier event architecture\n" +
                "  [green]•[/] Page-based navigation\n" +
                "  [green]•[/] Stateless handlers\n" +
                "  [green]•[/] Duplex event loop\n" +
                "  [green]•[/] Model layer integration\n\n" +
                "[grey]Press Escape to go back[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Aqua)
        );
    }
}

using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Components;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// Page for the counter demo.
/// Demonstrates subscribing to ViewModel properties.
/// </summary>
public class CounterPage : ReactivePage<CounterViewModel>
{
    private readonly StatusBar _statusBar = new()
    {
        Hints = "[↑] Increment [↓] Decrement [R] Reset [T] Todos [Q] Quit"
    };

    private int _currentCount;

    protected override void OnBound()
    {
        // Subscribe to count changes
        ViewModel.CountChanged
            .Subscribe(count => _currentCount = count)
            .DisposeWith(Subscriptions);

        // Subscribe to status message changes
        ViewModel.StatusMessageChanged
            .Subscribe(msg => _statusBar.Message = msg)
            .DisposeWith(Subscriptions);
    }

    public override IRenderable Render()
    {
        var counterDisplay = new Panel(
            new FigletText(_currentCount.ToString())
                .Centered()
                .Color(Color.Cyan1))
            .Header("[bold]Counter Demo[/]")
            .Expand()
            .Border(BoxBorder.Double)
            .BorderColor(Color.Blue);

        return new Rows(
            counterDisplay,
            _statusBar.Render()
        );
    }
}

using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// ViewModel for a simple counter demo.
/// Demonstrates reactive properties and input handling.
/// </summary>
public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _statusMessage = "Press Up/Down to change count, T for todos, Q to quit";

    public override void OnActivated()
    {
        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);
    }

    private void HandleKeyPress(KeyPressed key)
    {
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                Count++;
                StatusMessage = $"Incremented to {Count}";
                break;

            case ConsoleKey.DownArrow:
                Count--;
                StatusMessage = $"Decremented to {Count}";
                break;

            case ConsoleKey.R:
                Count = 0;
                StatusMessage = "Counter reset";
                break;

            case ConsoleKey.T:
                Navigate("/todos");
                break;

            case ConsoleKey.Q:
                Shutdown();
                break;
        }
    }
}

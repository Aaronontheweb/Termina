// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.V2;

/// <summary>
/// ViewModel for the counter demo.
/// Demonstrates reactive properties with the v2 rendering infrastructure.
/// </summary>
public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _statusMessage = "Press Up/Down to change count, Enter to submit message, Escape to quit";
    [Reactive] private string _inputText = "";
    [Reactive] private List<string> _messages = new();

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

            case ConsoleKey.Enter:
                if (!string.IsNullOrWhiteSpace(InputText))
                {
                    var newMessages = new List<string>(Messages)
                    {
                        $"[{DateTime.Now:HH:mm:ss}] {InputText}"
                    };
                    // Keep last 8 messages
                    if (newMessages.Count > 8)
                        newMessages.RemoveAt(0);
                    Messages = newMessages;
                    InputText = "";
                    StatusMessage = "Message sent!";
                }
                break;

            case ConsoleKey.Backspace:
                if (InputText.Length > 0)
                    InputText = InputText[..^1];
                break;

            case ConsoleKey.Escape:
                Shutdown();
                break;

            default:
                // Handle printable characters for input
                if (key.KeyInfo.KeyChar >= 32 && key.KeyInfo.KeyChar < 127)
                {
                    InputText += key.KeyInfo.KeyChar;
                }
                break;
        }
    }
}

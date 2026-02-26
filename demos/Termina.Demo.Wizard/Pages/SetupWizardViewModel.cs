// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Wizard.Pages;

public enum SetupStep
{
    Provider,
    Auth,
    Confirm
}

/// <summary>
/// ViewModel for the setup wizard demo.
/// </summary>
public class SetupWizardViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> SelectedProvider { get; } = new("None");
    public ReactiveProperty<string> Username { get; } = new("");
    public ReactiveProperty<string> StatusMessage { get; } = new("Use Enter to advance, Escape to go back");

    public override void OnActivated()
    {
        // Handle Q to quit at the ViewModel level
        Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(key =>
            {
                if (key.KeyInfo.Key == ConsoleKey.Q)
                    Shutdown();
            })
            .DisposeWith(Subscriptions);
    }

    public override void Dispose()
    {
        SelectedProvider.Dispose();
        Username.Dispose();
        StatusMessage.Dispose();
        base.Dispose();
    }
}

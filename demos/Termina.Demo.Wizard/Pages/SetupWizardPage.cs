// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Wizard.Pages;

/// <summary>
/// Page for the setup wizard demo.
/// Showcases WizardNode, DynamicLayoutNode (internal), FocusPolicy, and Tab cycling.
/// </summary>
public class SetupWizardPage : ReactivePage<SetupWizardViewModel>
{
    private WizardNode<SetupStep> _wizard = null!;

    public SetupWizardPage()
    {
        FocusPolicy = FocusPolicy.FirstFocusable;
    }

    public override ILayoutNode BuildLayout()
    {
        _wizard = Layouts.Wizard<SetupStep>()
            .WithStep(SetupStep.Provider, "Provider", BuildProviderStep,
                helpText: "Select a cloud provider and press Enter")
            .WithStep(SetupStep.Auth, "Authentication", BuildAuthStep,
                helpText: "Enter your username and press Enter to continue")
            .WithStep(SetupStep.Confirm, "Confirm", BuildConfirmStep,
                helpText: "Press Enter to complete setup, Escape to go back")
            .WithProgressStyle(WizardProgressStyle.Arrow)
            .WithBorder(BorderStyle.Rounded, Color.Cyan);
        _wizard.Fill();

        // Update status on step change
        _wizard.StepChanged.Subscribe(step =>
            ViewModel.StatusMessage.Value = $"Now on: {step}");

        // Shut down on completion
        _wizard.Completed.Subscribe(_ =>
        {
            ViewModel.StatusMessage.Value = "Setup complete!";
            ViewModel.RequestRedraw();
        });

        return Layouts.Vertical(
            _wizard,
            ViewModel.StatusMessage
                .Select<string, ILayoutNode>(msg => new TextNode(msg).WithForeground(Color.BrightBlack))
                .AsLayout()
                .Height(1),
            new TextNode("[Q] Quit")
                .WithForeground(Color.DarkGray)
                .Height(1)
        );
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Tab, CycleFocusForward);
        KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, CycleFocusBackward);

        // Focus the wizard itself so it receives Enter/Escape
        Focus.PushFocus(_wizard);
    }

    private ILayoutNode BuildProviderStep()
    {
        var list = Layouts.SelectionList("AWS", "Azure", "GCP")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan);

        list.SelectionConfirmed.Subscribe(items =>
        {
            if (items.Count > 0)
            {
                ViewModel.SelectedProvider.Value = items[0];
                ViewModel.StatusMessage.Value = $"Selected: {items[0]}";
                _wizard.TryAdvance();
            }
        });

        return Layouts.Vertical(
            new TextNode("Choose your cloud provider:")
                .WithForeground(Color.White),
            list
        ).WithSpacing(1);
    }

    private ILayoutNode BuildAuthStep()
    {
        var usernameInput = new TextInputNode()
            .WithPlaceholder("Enter username...");

        usernameInput.Submitted.Subscribe(text =>
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                ViewModel.Username.Value = text.Trim();
                ViewModel.StatusMessage.Value = $"Username set: {text.Trim()}";
                _wizard.TryAdvance();
            }
        });

        return Layouts.Vertical(
            new TextNode("Enter your credentials:")
                .WithForeground(Color.White),
            new TextNode("Username:").WithForeground(Color.Gray),
            usernameInput
        ).WithSpacing(1);
    }

    private ILayoutNode BuildConfirmStep()
    {
        return Layouts.Vertical(
            new TextNode("Review your settings:")
                .WithForeground(Color.White)
                .Bold(),
            ViewModel.SelectedProvider
                .Select<string, ILayoutNode>(p =>
                    new TextNode($"  Provider: {p}").WithForeground(Color.Cyan))
                .AsLayout(),
            ViewModel.Username
                .Select<string, ILayoutNode>(u =>
                    new TextNode($"  Username: {u}").WithForeground(Color.Cyan))
                .AsLayout(),
            new TextNode(""),
            new TextNode("Press Enter to complete setup.")
                .WithForeground(Color.Green)
        );
    }
}

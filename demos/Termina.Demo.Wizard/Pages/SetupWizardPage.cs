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
                helpText: "[↑/↓] Navigate  [Enter] Select  [1-3] Quick Select")
            .WithStep(SetupStep.Auth, "Authentication", BuildAuthStep,
                helpText: "Type your username and press Enter")
            .WithStep(SetupStep.Confirm, "Confirm", BuildConfirmStep,
                helpText: "[Enter] Complete Setup  [Esc] Go Back")
            .WithProgressStyle(WizardProgressStyle.Arrow)
            .WithBorder(BorderStyle.Rounded, Color.Cyan);
        _wizard.Fill();

        // Update status on step change
        _wizard.StepChanged.Subscribe(step =>
            ViewModel.StatusMessage.Value = $"Step: {step}");

        // Shut down on completion
        _wizard.Completed.Subscribe(_ =>
        {
            ViewModel.StatusMessage.Value = "✓ Setup complete! Press Q to exit.";
            ViewModel.RequestRedraw();
        });

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Cloud Setup Wizard")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Magenta)
                    .WithContent(_wizard)
                    .Fill())
            .WithChild(
                Layouts.Horizontal(
                    ViewModel.StatusMessage
                        .Select<string, ILayoutNode>(msg => new TextNode(msg).WithForeground(Color.White))
                        .AsLayout()
                        .Fill(),
                    new TextNode("[Tab] Cycle Focus  [Q] Quit")
                        .WithForeground(Color.BrightBlack))
                .Height(1));
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Tab, CycleFocusForward);
        KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, CycleFocusBackward);

        // Focus the wizard itself so it receives and delegates input
        Focus.PushFocus(_wizard);
    }

    private ILayoutNode BuildProviderStep()
    {
        var list = Layouts.SelectionList("AWS", "Azure", "GCP")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithVisibleRows(6);

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
                .WithForeground(Color.BrightCyan)
                .Bold()
                .Height(1),
            new TextNode("Select a provider for your infrastructure deployment.")
                .WithForeground(Color.DarkGray)
                .Height(2),
            list
        );
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
                ViewModel.StatusMessage.Value = $"Username: {text.Trim()}";
                _wizard.TryAdvance();
            }
        });

        return Layouts.Vertical(
            new TextNode("Enter your credentials:")
                .WithForeground(Color.BrightCyan)
                .Bold()
                .Height(1),
            new TextNode("Provide the username for your cloud provider account.")
                .WithForeground(Color.DarkGray)
                .Height(2),
            new TextNode("Username:")
                .WithForeground(Color.Gray)
                .Height(1),
            Layouts.Horizontal(
                new TextNode("  ").Width(2),
                usernameInput.Fill()
            ).Height(1)
        );
    }

    private ILayoutNode BuildConfirmStep()
    {
        return Layouts.Vertical(
            new TextNode("Review your settings:")
                .WithForeground(Color.BrightCyan)
                .Bold()
                .Height(1),
            new TextNode("Verify the details below before completing setup.")
                .WithForeground(Color.DarkGray)
                .Height(2),
            ViewModel.SelectedProvider
                .Select<string, ILayoutNode>(p =>
                    new TextNode($"  Provider:  {p}").WithForeground(Color.Cyan))
                .AsLayout()
                .Height(1),
            ViewModel.Username
                .Select<string, ILayoutNode>(u =>
                    new TextNode($"  Username:  {u}").WithForeground(Color.Cyan))
                .AsLayout()
                .Height(1),
            new TextNode("").Height(1),
            new TextNode("Press Enter to complete setup.")
                .WithForeground(Color.Green)
                .Height(1)
        );
    }
}

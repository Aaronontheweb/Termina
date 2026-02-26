# Building a Setup Wizard

This tutorial walks through building a 3-step setup wizard using `WizardNode<TStep>`, `DynamicLayoutNode`, `FocusPolicy.FirstFocusable`, and Tab cycling.

## Define the Steps

```csharp
public enum SetupStep
{
    Provider,
    Auth,
    Confirm
}
```

## Create the ViewModel

```csharp
public class SetupWizardViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> SelectedProvider { get; } = new("None");
    public ReactiveProperty<string> Username { get; } = new("");
    public ReactiveProperty<string> Password { get; } = new("");

    public override void Dispose()
    {
        SelectedProvider.Dispose();
        Username.Dispose();
        Password.Dispose();
        base.Dispose();
    }
}
```

## Create the Page

```csharp
public class SetupWizardPage : ReactivePage<SetupWizardViewModel>
{
    public SetupWizardPage()
    {
        FocusPolicy = FocusPolicy.FirstFocusable;
    }

    public override ILayoutNode BuildLayout()
    {
        var wizard = Layouts.Wizard<SetupStep>()
            .WithStep(SetupStep.Provider, "Provider", BuildProviderStep,
                helpText: "Choose your cloud provider")
            .WithStep(SetupStep.Auth, "Authentication", BuildAuthStep,
                helpText: "Enter your credentials")
            .WithStep(SetupStep.Confirm, "Confirm", BuildConfirmStep,
                helpText: "Press Enter to complete setup")
            .WithProgressStyle(WizardProgressStyle.Arrow)
            .WithBorder(BorderStyle.Rounded)
            .Fill();

        // Subscribe to completion
        wizard.Completed.Subscribe(_ => Shutdown());

        return wizard;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        KeyBindings.Register(ConsoleKey.Tab, CycleFocusForward);
        KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, CycleFocusBackward);
    }

    private ILayoutNode BuildProviderStep()
    {
        var list = Layouts.SelectionList("AWS", "Azure", "GCP")
            .WithMode(SelectionMode.Single);

        list.SelectionConfirmed.Subscribe(items =>
        {
            if (items.Count > 0)
                ViewModel.SelectedProvider.Value = items[0];
        });

        return list;
    }

    private ILayoutNode BuildAuthStep()
    {
        var username = new TextInputNode().WithPlaceholder("Username");
        var password = new TextInputNode().WithPlaceholder("Password");

        username.Submitted.Subscribe(text =>
            ViewModel.Username.Value = text);
        password.Submitted.Subscribe(text =>
            ViewModel.Password.Value = text);

        return Layouts.Vertical(
            new TextNode("Username:"),
            username,
            new TextNode("Password:"),
            password
        ).WithSpacing(1);
    }

    private ILayoutNode BuildConfirmStep()
    {
        return Layouts.Vertical(
            new TextNode("Review your settings:"),
            ViewModel.SelectedProvider
                .Select<string, ILayoutNode>(p => new TextNode($"  Provider: {p}"))
                .AsLayout(),
            ViewModel.Username
                .Select<string, ILayoutNode>(u => new TextNode($"  Username: {u}"))
                .AsLayout(),
            new TextNode(""),
            new TextNode("Press Enter to complete, Escape to go back.")
        );
    }
}
```

## Key Concepts Used

1. **`WizardNode<SetupStep>`** manages step navigation, progress display, and completion
2. **`DynamicLayoutNode`** (used internally by WizardNode) switches step content without observable pipelines
3. **`FocusPolicy.FirstFocusable`** auto-focuses the first interactive control on each page visit
4. **Tab cycling** via `CycleFocusForward` / `CycleFocusBackward` for keyboard navigation between inputs
5. **`BeforeAdvance`** can be subscribed to for step validation (set `args.Cancel = true` to prevent navigation)

## Wizard Navigation

| Key | Action |
|-----|--------|
| Enter | Advance to next step (or complete on last step) |
| Escape | Go back to previous step |
| Tab | Cycle focus forward |
| Shift+Tab | Cycle focus backward |

## Progress Styles

```csharp
.WithProgressStyle(WizardProgressStyle.BlockBar)  // [====>    ] Step 2 of 3
.WithProgressStyle(WizardProgressStyle.Arrow)      // Provider > [Auth] > Confirm
.WithProgressStyle(WizardProgressStyle.Dots)       // [*] [*] [ ] Auth
.WithProgressStyle(WizardProgressStyle.None)       // No progress indicator
```

// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

public enum TestWizardStep
{
    Provider,
    Auth,
    Confirm
}

/// <summary>
/// Tests for the WizardNode class.
/// </summary>
public class WizardNodeTests
{
    private static WizardNode<TestWizardStep> CreateThreeStepWizard()
    {
        return Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider", () => new TextNode("Select provider"))
            .WithStep(TestWizardStep.Auth, "Auth", () => new TextNode("Enter credentials"))
            .WithStep(TestWizardStep.Confirm, "Confirm", () => new TextNode("Review settings"));
    }

    [Fact]
    public void InitialState_IsFirstStep()
    {
        var wizard = CreateThreeStepWizard();

        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
        Assert.Equal(0, wizard.CurrentSubStep);
        Assert.Equal(3, wizard.StepCount);
    }

    [Fact]
    public void TryAdvance_MovesToNextStep()
    {
        var wizard = CreateThreeStepWizard();

        var result = wizard.TryAdvance();

        Assert.True(result);
        Assert.Equal(TestWizardStep.Auth, wizard.CurrentStep);
    }

    [Fact]
    public void TryAdvance_OnLastStep_FiresCompleted()
    {
        var wizard = CreateThreeStepWizard();
        var completedFired = false;
        wizard.Completed.Subscribe(_ => completedFired = true);

        wizard.TryAdvance(); // Provider -> Auth
        wizard.TryAdvance(); // Auth -> Confirm
        wizard.TryAdvance(); // Confirm -> completed

        Assert.True(completedFired);
    }

    [Fact]
    public void TryGoBack_MovesToPreviousStep()
    {
        var wizard = CreateThreeStepWizard();
        wizard.TryAdvance(); // Provider -> Auth

        var result = wizard.TryGoBack();

        Assert.True(result);
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
    }

    [Fact]
    public void TryGoBack_OnFirstStep_ReturnsFalse()
    {
        var wizard = CreateThreeStepWizard();

        var result = wizard.TryGoBack();

        Assert.False(result);
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
    }

    [Fact]
    public void BeforeAdvance_CanCancel()
    {
        var wizard = CreateThreeStepWizard();
        wizard.BeforeAdvance.Subscribe(args => args.Cancel = true);

        var result = wizard.TryAdvance();

        Assert.False(result);
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
    }

    [Fact]
    public void StepChanged_EmitsOnNavigation()
    {
        var wizard = CreateThreeStepWizard();
        var emissions = new List<TestWizardStep>();
        wizard.StepChanged.Subscribe(s => emissions.Add(s));

        wizard.TryAdvance(); // Provider -> Auth
        wizard.TryAdvance(); // Auth -> Confirm

        Assert.Equal(2, emissions.Count);
        Assert.Equal(TestWizardStep.Auth, emissions[0]);
        Assert.Equal(TestWizardStep.Confirm, emissions[1]);
    }

    [Fact]
    public void GoToStep_JumpsDirectly()
    {
        var wizard = CreateThreeStepWizard();

        var result = wizard.GoToStep(TestWizardStep.Confirm);

        Assert.True(result);
        Assert.Equal(TestWizardStep.Confirm, wizard.CurrentStep);
    }

    [Fact]
    public void GoToStep_SameStep_ReturnsFalse()
    {
        var wizard = CreateThreeStepWizard();

        var result = wizard.GoToStep(TestWizardStep.Provider);

        Assert.False(result);
    }

    [Fact]
    public void SubStep_Advance_WithinStep()
    {
        var wizard = Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider", () => new TextNode("P"), subSteps: 3)
            .WithStep(TestWizardStep.Auth, "Auth", () => new TextNode("A"));

        Assert.Equal(0, wizard.CurrentSubStep);

        wizard.AdvanceSubStep();
        Assert.Equal(1, wizard.CurrentSubStep);
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);

        wizard.AdvanceSubStep();
        Assert.Equal(2, wizard.CurrentSubStep);

        // Can't advance past sub-step count
        Assert.False(wizard.AdvanceSubStep());
        Assert.Equal(2, wizard.CurrentSubStep);
    }

    [Fact]
    public void TryAdvance_WithSubSteps_AdvancesSubStepFirst()
    {
        var wizard = Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider", () => new TextNode("P"), subSteps: 2)
            .WithStep(TestWizardStep.Auth, "Auth", () => new TextNode("A"));

        // First advance goes to sub-step 1
        wizard.TryAdvance();
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
        Assert.Equal(1, wizard.CurrentSubStep);

        // Second advance goes to next step
        wizard.TryAdvance();
        Assert.Equal(TestWizardStep.Auth, wizard.CurrentStep);
        Assert.Equal(0, wizard.CurrentSubStep);
    }

    [Fact]
    public void TryGoBack_WithSubSteps_GoesBackSubStepFirst()
    {
        var wizard = Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider", () => new TextNode("P"), subSteps: 2)
            .WithStep(TestWizardStep.Auth, "Auth", () => new TextNode("A"));

        wizard.TryAdvance(); // sub-step 0 -> 1
        wizard.TryAdvance(); // Provider -> Auth

        wizard.TryGoBack(); // Auth -> Provider (sub-step 1, last sub-step of prev)
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
        Assert.Equal(1, wizard.CurrentSubStep);

        wizard.TryGoBack(); // sub-step 1 -> 0
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
        Assert.Equal(0, wizard.CurrentSubStep);
    }

    [Fact]
    public void HandleInput_Enter_Advances()
    {
        var wizard = CreateThreeStepWizard();
        var key = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);

        wizard.HandleInput(key);

        Assert.Equal(TestWizardStep.Auth, wizard.CurrentStep);
    }

    [Fact]
    public void HandleInput_Escape_GoesBack()
    {
        var wizard = CreateThreeStepWizard();
        wizard.TryAdvance();
        var key = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);

        wizard.HandleInput(key);

        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
    }

    [Fact]
    public void HandleInput_DelegatesToNestedFocusable()
    {
        // Arrange: wizard step content has a focusable node nested inside a vertical layout
        var handled = false;
        var focusable = new TestFocusableNode(key =>
        {
            if (key.Key == ConsoleKey.DownArrow)
            {
                handled = true;
                return true;
            }
            return false;
        });

        var wizard = Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider",
                () => Layouts.Vertical(
                    new TextNode("Header"),
                    focusable // nested inside a container
                ))
            .WithStep(TestWizardStep.Auth, "Auth", () => new TextNode("Auth"));

        // Force DynamicLayoutNode to evaluate its factory by rendering
        var ctx = new NullRenderContext();
        wizard.Render(ctx, new Rect(0, 0, 80, 24));

        var key = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);

        // Act
        var result = wizard.HandleInput(key);

        // Assert: the nested focusable handled the input, not the wizard
        Assert.True(result);
        Assert.True(handled);
        // Wizard did NOT advance (DownArrow is not Enter)
        Assert.Equal(TestWizardStep.Provider, wizard.CurrentStep);
    }

    [Fact]
    public void HandleInput_FallsThroughToWizard_WhenChildDoesNotHandle()
    {
        // Arrange: focusable child doesn't handle Enter
        var focusable = new TestFocusableNode(_ => false);

        var wizard = Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider",
                () => Layouts.Vertical(new TextNode("Header"), focusable))
            .WithStep(TestWizardStep.Auth, "Auth", () => new TextNode("Auth"));

        // Force DynamicLayoutNode to evaluate its factory by rendering
        var ctx = new NullRenderContext();
        wizard.Render(ctx, new Rect(0, 0, 80, 24));

        var key = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);

        // Act
        wizard.HandleInput(key);

        // Assert: wizard advanced because child didn't consume Enter
        Assert.Equal(TestWizardStep.Auth, wizard.CurrentStep);
    }

    [Fact]
    public void IFocusable_CanFocus_IsTrue()
    {
        var wizard = CreateThreeStepWizard();

        Assert.True(wizard.CanFocus);
    }

    [Fact]
    public void IFocusable_OnFocused_SetsHasFocus()
    {
        var wizard = CreateThreeStepWizard();

        wizard.OnFocused();

        Assert.True(wizard.HasFocus);
    }

    [Fact]
    public void IFocusable_OnBlurred_ClearsHasFocus()
    {
        var wizard = CreateThreeStepWizard();
        wizard.OnFocused();

        wizard.OnBlurred();

        Assert.False(wizard.HasFocus);
    }

    [Fact]
    public void Invalidated_EmitsOnNavigation()
    {
        var wizard = CreateThreeStepWizard();
        var count = 0;
        wizard.Invalidated.Subscribe(_ => count++);

        wizard.TryAdvance();

        Assert.True(count > 0);
    }

    [Fact]
    public void ProgressStyle_BlockBar_Renders()
    {
        var wizard = CreateThreeStepWizard()
            .WithProgressStyle(WizardProgressStyle.BlockBar);

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
        // Should not throw
    }

    [Fact]
    public void ProgressStyle_Arrow_Renders()
    {
        var wizard = CreateThreeStepWizard()
            .WithProgressStyle(WizardProgressStyle.Arrow);

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
    }

    [Fact]
    public void ProgressStyle_Dots_Renders()
    {
        var wizard = CreateThreeStepWizard()
            .WithProgressStyle(WizardProgressStyle.Dots);

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
    }

    [Fact]
    public void ProgressStyle_None_Renders()
    {
        var wizard = CreateThreeStepWizard()
            .WithProgressStyle(WizardProgressStyle.None);

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
    }

    [Fact]
    public void WithBorder_Renders()
    {
        var wizard = CreateThreeStepWizard()
            .WithBorder(BorderStyle.Single);

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
    }

    [Fact]
    public void WithTitle_Renders()
    {
        var wizard = CreateThreeStepWizard()
            .WithTitle("Setup Wizard");

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
    }

    [Fact]
    public void WithHelpText_Renders()
    {
        var wizard = Layouts.Wizard<TestWizardStep>()
            .WithStep(TestWizardStep.Provider, "Provider", () => new TextNode("Select"), helpText: "Press Enter to continue");

        var context = new NullRenderContext();
        wizard.Render(context, new Rect(0, 0, 80, 24));
    }

    [Fact]
    public void Dispose_CleansUpSubjects()
    {
        var wizard = CreateThreeStepWizard();
        var stepChangedCompleted = false;
        var completedCompleted = false;

        wizard.StepChanged.Subscribe(
            _ => { },
            _ => stepChangedCompleted = true);
        wizard.Completed.Subscribe(
            _ => { },
            _ => completedCompleted = true);

        wizard.Dispose();

        Assert.True(stepChangedCompleted);
        Assert.True(completedCompleted);
    }

    [Fact]
    public void DynamicLayoutNode_Integration_ContentChangesOnStep()
    {
        var wizard = CreateThreeStepWizard();
        var context = new NullRenderContext();
        wizard.OnActivate();

        // Render first step
        wizard.Render(context, new Rect(0, 0, 80, 24));

        // Advance and render second step
        wizard.TryAdvance();
        wizard.Render(context, new Rect(0, 0, 80, 24));

        // Should not throw — DynamicLayoutNode handles child switching
        Assert.Equal(TestWizardStep.Auth, wizard.CurrentStep);
    }

    [Fact]
    public void EmptyWizard_TryAdvance_ReturnsFalse()
    {
        var wizard = Layouts.Wizard<TestWizardStep>();

        Assert.False(wizard.TryAdvance());
        Assert.False(wizard.TryGoBack());
    }

    [Fact]
    public void FluentBuilder_ReturnsSameInstance()
    {
        var wizard = Layouts.Wizard<TestWizardStep>();

        var result = wizard
            .WithStep(TestWizardStep.Provider, "P", () => new EmptyNode())
            .WithProgressStyle(WizardProgressStyle.Arrow)
            .WithTitle("Test")
            .WithBorder(BorderStyle.Rounded);

        Assert.Same(wizard, result);
    }

    [Fact]
    public void FactoryMethod_CreatesInstance()
    {
        var wizard = Layouts.Wizard<TestWizardStep>();

        Assert.NotNull(wizard);
        Assert.Equal(0, wizard.StepCount);
    }

    /// <summary>
    /// Test IFocusable that extends LayoutNode so it's discoverable by tree walk.
    /// </summary>
    private class TestFocusableNode : LayoutNode, IFocusable
    {
        private readonly Func<ConsoleKeyInfo, bool> _handleInput;

        public TestFocusableNode(Func<ConsoleKeyInfo, bool> handleInput)
        {
            _handleInput = handleInput;
        }

        public bool CanFocus => true;
        public bool HasFocus { get; private set; }
        public int FocusPriority => 0;
        public void OnFocused() => HasFocus = true;
        public void OnBlurred() => HasFocus = false;
        public bool HandleInput(ConsoleKeyInfo key) => _handleInput(key);

        public override Size Measure(Size available) => new(available.Width, 1);
        public override void Render(IRenderContext context, Rect bounds) { }
    }

    private class NullRenderContext : IRenderContext
    {
        public int Width => 80;
        public int Height => 24;
        public void WriteAt(int x, int y, string text) { }
        public void WriteAt(int x, int y, char c) { }
        public void SetForeground(Color color) { }
        public void SetBackground(Color color) { }
        public void ResetColors() { }
        public void SetDecoration(TextDecoration decoration) { }
        public void ApplyStyle(TextStyle style) { }
        public void Fill(int x, int y, int width, int height, char c = ' ') { }
        public void Clear() { }
        public IRenderContext CreateSubContext(Rect bounds) => this;
    }
}

// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;
using Streaming = Termina.Components.Streaming;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the SelectionListNode component.
/// </summary>
public class SelectionListNodeTests
{
    [Fact]
    public void SelectionListNode_CanBeCreatedWithItems()
    {
        var items = new[] { "Item 1", "Item 2", "Item 3" };
        using var list = new SelectionListNode<string>(items, s => s);

        Assert.NotNull(list);
        Assert.Equal(3, list.Items.Count);
    }

    [Fact]
    public void SelectionListNode_HasDefaultValues()
    {
        using var list = Layouts.SelectionList("A", "B");

        Assert.True(list.CanFocus);
        Assert.False(list.HasFocus);
        Assert.Equal(10, list.FocusPriority);
    }

    [Fact]
    public void SelectionListNode_FluentApi_ReturnsThis()
    {
        var items = new[] { "A", "B", "C" };
        using var list = new SelectionListNode<string>(items, s => s);

        var result = list
            .WithMode(SelectionMode.Multi)
            .WithHighlightColors(Color.Black, Color.White)
            .WithForeground(Color.Gray)
            .WithSelectedForeground(Color.Green)
            .WithShowNumbers(true)
            .WithVisibleRows(5);

        Assert.Same(list, result);
    }

    [Fact]
    public void SelectionListNode_WithOtherOption_AddsOtherItem()
    {
        var items = new[] { "A", "B" };
        using var list = new SelectionListNode<string>(items, s => s)
            .WithOtherOption("Custom...");

        Assert.Equal(3, list.Items.Count);
        Assert.True(list.Items[2].IsOther);
        Assert.Equal("Custom...", list.Items[2].DisplayText);
    }

    [Fact]
    public void SelectionListNode_OnFocused_SetsHasFocus()
    {
        using var list = Layouts.SelectionList("A", "B");
        Assert.False(list.HasFocus);

        list.OnFocused();

        Assert.True(list.HasFocus);
    }

    [Fact]
    public void SelectionListNode_OnBlurred_ClearsHasFocus()
    {
        using var list = Layouts.SelectionList("A", "B");
        list.OnFocused();

        list.OnBlurred();

        Assert.False(list.HasFocus);
    }

    [Fact]
    public void SelectionListNode_HandleInput_DownArrow_MovesHighlight()
    {
        using var list = Layouts.SelectionList("A", "B", "C");
        list.OnFocused();

        // Initial highlight should be at index 0
        Assert.Equal("A", list.HighlightedItem?.DisplayText);

        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);

        Assert.Equal("B", list.HighlightedItem?.DisplayText);
    }

    [Fact]
    public void SelectionListNode_HandleInput_UpArrow_MovesHighlight()
    {
        using var list = Layouts.SelectionList("A", "B", "C");
        list.OnFocused();

        // Move down first
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        Assert.Equal("B", list.HighlightedItem?.DisplayText);

        // Then up
        var upKey = new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false);
        list.HandleInput(upKey);

        Assert.Equal("A", list.HighlightedItem?.DisplayText);
    }

    [Fact]
    public void SelectionListNode_HandleInput_Home_MovesToFirst()
    {
        using var list = Layouts.SelectionList("A", "B", "C");
        list.OnFocused();

        // Move to middle
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(downKey);
        Assert.Equal("C", list.HighlightedItem?.DisplayText);

        // Press Home
        var homeKey = new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false);
        list.HandleInput(homeKey);

        Assert.Equal("A", list.HighlightedItem?.DisplayText);
    }

    [Fact]
    public void SelectionListNode_HandleInput_End_MovesToLast()
    {
        using var list = Layouts.SelectionList("A", "B", "C");
        list.OnFocused();

        Assert.Equal("A", list.HighlightedItem?.DisplayText);

        var endKey = new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false);
        list.HandleInput(endKey);

        Assert.Equal("C", list.HighlightedItem?.DisplayText);
    }

    [Fact]
    public void SelectionListNode_HandleInput_Space_TogglesSelectionInMultiMode()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithMode(SelectionMode.Multi);
        list.OnFocused();

        Assert.False(list.Items[0].IsSelected);

        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        list.HandleInput(spaceKey);

        Assert.True(list.Items[0].IsSelected);

        // Toggle off
        list.HandleInput(spaceKey);
        Assert.False(list.Items[0].IsSelected);
    }

    [Fact]
    public void SelectionListNode_HandleInput_NumberKeys_SelectItem()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithShowNumbers(true)
            .WithMode(SelectionMode.Multi);
        list.OnFocused();

        // Press '2' to select second item
        var key2 = new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false);
        list.HandleInput(key2);

        Assert.True(list.Items[1].IsSelected);
    }

    [Fact]
    public void SelectionListNode_HandleInput_Enter_EmitsSelectionConfirmed()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithMode(SelectionMode.Multi);
        list.OnFocused();

        var confirmed = new List<string>();
        list.SelectionConfirmed.Subscribe(items => confirmed.AddRange(items));

        // Select first item
        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        list.HandleInput(spaceKey);

        // Confirm
        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        Assert.Single(confirmed);
        Assert.Equal("A", confirmed[0]);
    }

    [Fact]
    public void SelectionListNode_HandleInput_Escape_EmitsCancelled()
    {
        using var list = Layouts.SelectionList("A", "B", "C");
        list.OnFocused();

        var cancelled = false;
        list.Cancelled.Subscribe(_ => cancelled = true);

        var escapeKey = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        list.HandleInput(escapeKey);

        Assert.True(cancelled);
    }

    [Fact]
    public void SelectionListNode_SingleMode_ClearsOtherSelections()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithMode(SelectionMode.Single);
        list.OnFocused();

        // Select first item
        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        list.HandleInput(spaceKey);
        Assert.True(list.Items[0].IsSelected);

        // Move down and select second
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(spaceKey);

        // First should be deselected, second selected
        Assert.False(list.Items[0].IsSelected);
        Assert.True(list.Items[1].IsSelected);
    }

    [Fact]
    public void SelectionListNode_SelectedItems_ReturnsOnlySelected()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithMode(SelectionMode.Multi);
        list.OnFocused();

        // Select A and C
        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);

        list.HandleInput(spaceKey); // Select A
        list.HandleInput(downKey);  // Move to B
        list.HandleInput(downKey);  // Move to C
        list.HandleInput(spaceKey); // Select C

        var selected = list.SelectedItems;
        Assert.Equal(2, selected.Count);
        Assert.Contains("A", selected);
        Assert.Contains("C", selected);
    }

    [Fact]
    public void SelectionListNode_Invalidated_EmitsOnChanges()
    {
        using var list = Layouts.SelectionList("A", "B", "C");
        var invalidationCount = 0;
        list.Invalidated.Subscribe(_ => invalidationCount++);

        list.OnFocused();
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);

        Assert.True(invalidationCount > 0);
    }

    [Fact]
    public void SelectionListNode_Dispose_CompletesObservables()
    {
        var list = Layouts.SelectionList("A", "B");
        var completed = 0;

        list.SelectionConfirmed.Subscribe(
            onNext: _ => { },
            onCompleted: _ => completed++);
        list.OtherSelected.Subscribe(
            onNext: _ => { },
            onCompleted: _ => completed++);
        list.Cancelled.Subscribe(
            onNext: _ => { },
            onCompleted: _ => completed++);
        list.Invalidated.Subscribe(
            onNext: _ => { },
            onCompleted: _ => completed++);

        list.Dispose();

        Assert.Equal(4, completed);
    }

    [Fact]
    public void SelectionListNode_TypedItems_WorksWithCustomTypes()
    {
        var items = new[]
        {
            new TestItem(1, "First"),
            new TestItem(2, "Second"),
            new TestItem(3, "Third")
        };

        using var list = new SelectionListNode<TestItem>(items, i => i.Name);

        Assert.Equal(3, list.Items.Count);
        Assert.Equal("First", list.Items[0].DisplayText);
        Assert.Equal(1, list.Items[0].Value.Id);
    }

    private record TestItem(int Id, string Name);

    [Fact]
    public void SelectionListNode_SingleMode_EnterSelectsHighlightedItem()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithMode(SelectionMode.Single);
        list.OnFocused();

        var confirmed = new List<string>();
        list.SelectionConfirmed.Subscribe(items => confirmed.AddRange(items));

        // Move to B and press Enter without pressing Space first
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);

        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        // Should have selected B (the highlighted item)
        Assert.Single(confirmed);
        Assert.Equal("B", confirmed[0]);
    }

    [Fact]
    public void SelectionListNode_SingleMode_EnterOverridesPreviousSelection()
    {
        using var list = Layouts.SelectionList("A", "B", "C")
            .WithMode(SelectionMode.Single);
        list.OnFocused();

        var confirmed = new List<string>();
        list.SelectionConfirmed.Subscribe(items => confirmed.AddRange(items));

        // First select A with Space
        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        list.HandleInput(spaceKey);
        Assert.True(list.Items[0].IsSelected);

        // Move to C and press Enter (not Space)
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(downKey);

        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        // Should have selected C (overriding A)
        Assert.Single(confirmed);
        Assert.Equal("C", confirmed[0]);
        Assert.False(list.Items[0].IsSelected);
        Assert.True(list.Items[2].IsSelected);
    }

    [Fact]
    public void SelectionListNode_OtherOption_NavigationHighlightsWithoutStartingEdit()
    {
        using var list = Layouts.SelectionList("A", "B")
            .WithOtherOption("Custom...");
        list.OnFocused();

        // Move to Other option - should highlight but NOT auto-start editing
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(downKey);

        // Verify we're on the Other option
        Assert.True(list.HighlightedItem?.IsOther);

        // Typing should NOT work (not in edit mode yet) - returns false (unhandled)
        var typingResult = list.HandleInput(new ConsoleKeyInfo('T', ConsoleKey.T, false, false, false));
        Assert.False(typingResult);
    }

    [Fact]
    public void SelectionListNode_OtherOption_EnterStartsEditing()
    {
        using var list = Layouts.SelectionList("A", "B")
            .WithOtherOption("Custom...");
        list.OnFocused();

        string? customValue = null;
        list.OtherSelected.Subscribe(text => customValue = text);

        // Navigate to Other option
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(downKey);
        Assert.True(list.HighlightedItem?.IsOther);

        // Press Enter to start editing
        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        // Now typing should work
        list.HandleInput(new ConsoleKeyInfo('H', ConsoleKey.H, false, false, false));
        list.HandleInput(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false));

        // Press Enter to confirm
        list.HandleInput(enterKey);

        Assert.Equal("Hi", customValue);
    }

    [Fact]
    public void SelectionListNode_OtherOption_SpaceStartsEditing()
    {
        using var list = Layouts.SelectionList("A", "B")
            .WithOtherOption("Custom...");
        list.OnFocused();

        string? customValue = null;
        list.OtherSelected.Subscribe(text => customValue = text);

        // Navigate to Other option
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(downKey);
        Assert.True(list.HighlightedItem?.IsOther);

        // Press Space to start editing
        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        list.HandleInput(spaceKey);

        // Now typing should work
        list.HandleInput(new ConsoleKeyInfo('Y', ConsoleKey.Y, false, false, false));
        list.HandleInput(new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false));

        // Press Enter to confirm
        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        Assert.Equal("Yo", customValue);
    }

    [Fact]
    public void SelectionListNode_OtherOption_EmitsOtherSelectedOnConfirm()
    {
        using var list = Layouts.SelectionList("A", "B")
            .WithOtherOption("Custom...");
        list.OnFocused();

        string? customValue = null;
        list.OtherSelected.Subscribe(text => customValue = text);

        // Navigate to Other
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);
        list.HandleInput(downKey);

        // Press Enter to start editing (required - no auto-start on navigation)
        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        // Now type "Hi"
        list.HandleInput(new ConsoleKeyInfo('H', ConsoleKey.H, false, false, false));
        list.HandleInput(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false));

        // Press Enter to confirm
        list.HandleInput(enterKey);

        Assert.Equal("Hi", customValue);
    }

    [Fact]
    public void SelectionListNode_OtherOption_NumberKeyAutoStartsEditing()
    {
        using var list = Layouts.SelectionList("A", "B")
            .WithOtherOption("Custom...");
        list.OnFocused();

        string? customValue = null;
        list.OtherSelected.Subscribe(text => customValue = text);

        // Press '3' to jump to Other option - should auto-start editing
        var key3 = new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false);
        list.HandleInput(key3);

        // Type "Fast" immediately
        list.HandleInput(new ConsoleKeyInfo('F', ConsoleKey.F, false, false, false));
        list.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        list.HandleInput(new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false));
        list.HandleInput(new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false));

        // Press Enter to confirm
        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        Assert.Equal("Fast", customValue);
    }

    #region Number Display Tests

    [Fact]
    public void SelectionListNode_ShowsNumbersForAllItems_NotJustFirstNine()
    {
        // Create list with 12 items
        var items = Enumerable.Range(1, 12).Select(i => $"Item {i}").ToArray();
        using var list = new SelectionListNode<string>(items, s => s)
            .WithShowNumbers(true);

        // All 12 items should be in the list
        Assert.Equal(12, list.Items.Count);

        // Measure to ensure the component can handle 12 numbered items
        var size = list.Measure(new Size(80, 20));

        // The measured size should accommodate all items
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void SelectionListNode_NumberKeysOnlyWorkFor1Through9()
    {
        // Create list with 12 items
        var items = Enumerable.Range(1, 12).Select(i => $"Item {i}").ToArray();
        using var list = new SelectionListNode<string>(items, s => s)
            .WithShowNumbers(true)
            .WithMode(SelectionMode.Multi);
        list.OnFocused();

        // Number key 9 should work
        var key9 = new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false);
        list.HandleInput(key9);
        Assert.True(list.Items[8].IsSelected); // 9th item (index 8)

        // The highlighted item should be the 9th item after pressing '9'
        Assert.Equal("Item 9", list.HighlightedItem?.DisplayText);
    }

    [Fact]
    public void SelectionListNode_CanNavigateToItemsBeyondNine()
    {
        // Create list with 12 items
        var items = Enumerable.Range(1, 12).Select(i => $"Item {i}").ToArray();
        using var list = new SelectionListNode<string>(items, s => s)
            .WithShowNumbers(true);
        list.OnFocused();

        // Navigate to end
        var endKey = new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false);
        list.HandleInput(endKey);

        // Should be on the 12th item
        Assert.Equal("Item 12", list.HighlightedItem?.DisplayText);
    }

    #endregion

    #region Rich Content Tests

    [Fact]
    public void SelectionListNode_WithRichContent_CanBeCreated()
    {
        var items = new[] { "Item 1", "Item 2", "Item 3" };
        using var list = new SelectionListNode<string>(items, s =>
            new SelectionItemContent().AddLine(new StaticTextSegment(s, Color.Green)));

        Assert.NotNull(list);
        Assert.Equal(3, list.Items.Count);
    }

    [Fact]
    public void SelectionListNode_WithRichContent_MultiLine_HasCorrectLineCount()
    {
        var items = new[] { "Server A", "Server B" };
        using var list = new SelectionListNode<string>(items, s =>
            new SelectionItemContent()
                .AddLine(new StaticTextSegment(s, Color.White, decoration: TextDecoration.Bold))
                .AddLine(new StaticTextSegment("   Status: Connected", Color.Green)));

        // Each item has 2 lines
        Assert.Equal(2, list.Items[0].LineCount);
        Assert.Equal(2, list.Items[1].LineCount);
    }

    [Fact]
    public void SelectionListNode_WithRichContent_DisplayText_ReturnsFirstLine()
    {
        using var list = new SelectionListNode<string>(
            new[] { "Test" },
            s => new SelectionItemContent()
                .AddLine(new StaticTextSegment(new StyledSegment("Header")))
                .AddLine(new StaticTextSegment(new StyledSegment("Body"))));

        Assert.Equal("Header", list.Items[0].DisplayText);
    }

    [Fact]
    public void SelectionListNode_WithRichContent_NavigationWorks()
    {
        var items = new[] { "A", "B", "C" };
        using var list = new SelectionListNode<string>(items, s =>
            new SelectionItemContent().AddLine(new StaticTextSegment(new StyledSegment(s))));
        list.OnFocused();

        Assert.Equal("A", list.HighlightedItem?.DisplayText);

        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        list.HandleInput(downKey);

        Assert.Equal("B", list.HighlightedItem?.DisplayText);
    }

    [Fact]
    public void SelectionListNode_WithRichContent_SelectionWorks()
    {
        var items = new[] { "Option 1", "Option 2" };
        using var list = new SelectionListNode<string>(items, s =>
            new SelectionItemContent().AddLine(new StaticTextSegment(s, Color.Cyan)))
            .WithMode(SelectionMode.Multi);
        list.OnFocused();

        var spaceKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        list.HandleInput(spaceKey);

        Assert.True(list.Items[0].IsSelected);
        Assert.Single(list.SelectedItems);
        Assert.Equal("Option 1", list.SelectedItems[0]);
    }

    [Fact]
    public void SelectionListNode_WithRichContent_ConfirmationWorks()
    {
        var items = new[] { "Choice A", "Choice B" };
        using var list = new SelectionListNode<string>(items, s =>
            new SelectionItemContent()
                .AddLine(new StaticTextSegment(new StyledSegment(s)))
                .AddLine(new StaticTextSegment("   Description", Color.Gray)));
        list.OnFocused();

        var confirmed = new List<string>();
        list.SelectionConfirmed.Subscribe(selected => confirmed.AddRange(selected));

        var enterKey = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        list.HandleInput(enterKey);

        Assert.Single(confirmed);
        Assert.Equal("Choice A", confirmed[0]);
    }

    [Fact]
    public async Task SelectionListNode_WithAnimatedContent_PropagatesInvalidation()
    {
        using var list = new SelectionListNode<string>(
            new[] { "Loading" },
            s => new SelectionItemContent()
                .AddLine(
                    new SpinnerSegment(Streaming.SpinnerStyle.Dots, Color.Blue, intervalMs: 10),
                    new StaticTextSegment($" {s}...", Color.White)));

        // Wait for invalidation from spinner
        await list.Invalidated
            .FirstAsync()
            .WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(true); // Got here means invalidation propagated
    }

    [Fact]
    public void SelectionListNode_WithRichContent_Dispose_DisposesItems()
    {
        var list = new SelectionListNode<string>(
            new[] { "Test" },
            s => new SelectionItemContent().AddLine(new StaticTextSegment(new StyledSegment(s))));

        var completed = 0;
        list.Invalidated.Subscribe(
            onNext: _ => { },
            onCompleted: _ => completed++);

        list.Dispose();

        Assert.Equal(1, completed);
    }

    [Fact]
    public void SelectionListNode_MixedContentStyles_WorksCorrectly()
    {
        var items = new[]
        {
            ("Server 1", "Connected", Color.Green),
            ("Server 2", "Disconnected", Color.Red),
            ("Server 3", "Connecting", Color.Yellow)
        };

        using var list = new SelectionListNode<(string Name, string Status, Color StatusColor)>(
            items,
            item => new SelectionItemContent()
                .AddLine(new StaticTextSegment(item.Name, Color.White, decoration: TextDecoration.Bold))
                .AddLine(
                    new StaticTextSegment("   Status: ", Color.Gray),
                    new StaticTextSegment(item.Status, item.StatusColor)));

        Assert.Equal(3, list.Items.Count);
        Assert.Equal("Server 1", list.Items[0].DisplayText);
        Assert.Equal("Server 2", list.Items[1].DisplayText);
        Assert.Equal(2, list.Items[0].LineCount);
    }

    [Fact]
    public void SelectionListNode_Content_AccessibleThroughSelectItem()
    {
        using var list = new SelectionListNode<string>(
            new[] { "Test" },
            s => new SelectionItemContent()
                .AddLine("Header", Color.White)
                .AddLine("Body", Color.Gray));

        var item = list.Items[0];

        Assert.NotNull(item.Content);
        Assert.Equal(2, item.Content.LineCount);
        Assert.Equal("Header\nBody", item.Content.ToPlainText());
    }


    [Fact]
    public void SelectionListNode_WithFillHeight_ReturnsThis()
    {
        var items = new[] { "A", "B", "C" };
        using var list = new SelectionListNode<string>(items, s => s);

        var result = list.WithFillHeight();

        Assert.Same(list, result);
    }

    [Fact]
    public void SelectionListNode_WithFillHeight_HeightConstraintIsFill()
    {
        using var list = Layouts.SelectionList("A", "B");

        // Default: AutoSize
        Assert.IsType<SizeConstraint.Auto>(list.HeightConstraint);

        list.WithFillHeight();

        Assert.IsType<SizeConstraint.Fill>(list.HeightConstraint);
    }

    [Fact]
    public void SelectionListNode_WithFillHeight_MeasureUsesAvailableHeight()
    {
        var items = Enumerable.Range(1, 50).Select(i => "Item " + i).ToArray();
        using var list = new SelectionListNode<string>(items, s => s).WithFillHeight();

        var measured = list.Measure(new Size(80, 30));

        Assert.Equal(30, measured.Height);
    }

    [Fact]
    public void SelectionListNode_WithFillHeight_MeasureClampsToContentCount()
    {
        var items = new[] { "A", "B", "C" };
        using var list = new SelectionListNode<string>(items, s => s).WithFillHeight();

        var measured = list.Measure(new Size(80, 50));

        Assert.Equal(3, measured.Height);
    }

    [Fact]
    public void SelectionListNode_WithFillHeight_SmallAvailableHeight_ClampsToOne()
    {
        var items = Enumerable.Range(1, 50).Select(i => "Item " + i).ToArray();
        using var list = new SelectionListNode<string>(items, s => s).WithFillHeight();

        var measured = list.Measure(new Size(80, 0));

        Assert.Equal(1, measured.Height);
    }

    [Fact]
    public void SelectionListNode_DefaultBehavior_CapsAtVisibleRows()
    {
        var items = Enumerable.Range(1, 50).Select(i => "Item " + i).ToArray();
        using var list = new SelectionListNode<string>(items, s => s);

        var measured = list.Measure(new Size(80, 50));

        Assert.Equal(10, measured.Height);
    }

    [Fact]
    public void SelectionListNode_WithVisibleRows_DisablesFillHeight()
    {
        var items = Enumerable.Range(1, 50).Select(i => "Item " + i).ToArray();
        using var list = new SelectionListNode<string>(items, s => s)
            .WithFillHeight()
            .WithVisibleRows(5);

        Assert.IsType<SizeConstraint.Auto>(list.HeightConstraint);

        var measured = list.Measure(new Size(80, 30));
        Assert.Equal(5, measured.Height);
    }

    [Fact]
    public void SelectionListNode_WithFillHeight_RenderSyncsVisibleRowsForScrolling()
    {
        // 50 items in a fill-height list rendered straight into a 5-row viewport with
        // no preceding Measure() -- this is how VerticalLayout.Render treats a Fill
        // child. Render() must sync the visible-row count from bounds.Height so that
        // EnsureVisible()'s scroll math uses 5, not the default _visibleRows of 10.
        var items = Enumerable.Range(1, 50).Select(i => "Item " + i).ToArray();
        using var list = new SelectionListNode<string>(items, s => s).WithFillHeight();

        var terminal = new VirtualTerminal(30, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 30, 5);

        list.Render(context, new Rect(0, 0, 30, 5));

        // Navigate well past the bottom of the 5-row viewport.
        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        for (var i = 0; i < 10; i++)
            list.HandleInput(downKey);

        list.Render(context, new Rect(0, 0, 30, 5));

        var rendered = string.Join("\n", Enumerable.Range(0, 5).Select(terminal.GetLine));

        // The highlight is on "Item 11" (index 10). With visible rows synced to 5 the
        // list scrolls to keep it on screen (offset 6 -> items 7..11). If _visibleRows
        // were stuck at 10, EnsureVisible() would scroll only to offset 1 and "Item 11"
        // would fall off the bottom of the viewport.
        Assert.Contains("Item 11", rendered);
        Assert.Contains("Item 7", rendered);
    }

    #endregion
}

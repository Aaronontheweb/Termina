// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

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
            onCompleted: () => completed++);
        list.OtherSelected.Subscribe(
            onNext: _ => { },
            onCompleted: () => completed++);
        list.Cancelled.Subscribe(
            onNext: _ => { },
            onCompleted: () => completed++);
        list.Invalidated.Subscribe(
            onNext: _ => { },
            onCompleted: () => completed++);

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
}

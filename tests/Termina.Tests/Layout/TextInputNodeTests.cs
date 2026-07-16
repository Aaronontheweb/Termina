// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Input;

using R3;
namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the TextInputNode layout component.
/// </summary>
public class TextInputNodeTests : IDisposable
{
    private readonly TextInputNode _node;

    public TextInputNodeTests()
    {
        _node = new TextInputNode();
    }

    public void Dispose()
    {
        _node.Dispose();
    }

    [Fact]
    public void HandleInput_UpArrow_ReturnsFalse_ForViewModelToHandle()
    {
        // Up arrow should return false so ViewModel can handle history
        var handled = _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.False(handled);
    }

    [Fact]
    public void HandleInput_DownArrow_ReturnsFalse_ForViewModelToHandle()
    {
        // Down arrow should return false so ViewModel can handle history
        var handled = _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Assert.False(handled);
    }

    [Fact]
    public void HandleInput_Enter_ClearsTextAfterSubmit()
    {
        // Type some text
        TypeText("test text");

        string? submitted = null;
        _node.Submitted.Subscribe(t => submitted = t);

        // Press Enter — submits and clears
        _node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("test text", submitted);
        Assert.Equal("", _node.Text);
    }

    [Fact]
    public void Clear_ResetsTextAndCursor()
    {
        // Type some text
        TypeText("test text");

        // Clear
        _node.Clear();

        // Text should be cleared
        Assert.Equal("", _node.Text);
    }

    [Fact]
    public void Submitted_EventFired_OnEnter()
    {
        string? submittedText = null;
        _node.Submitted.Subscribe(text => submittedText = text);

        TypeText("test submission");
        _node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("test submission", submittedText);
    }

    [Fact]
    public void TextChanged_EventFired_OnCharacterInput()
    {
        var changeCount = 0;
        _node.TextChanged.Subscribe(_ => changeCount++);

        TypeText("abc");

        Assert.Equal(3, changeCount);
    }

    [Fact]
    public void TextChanged_EventFired_OnClear()
    {
        TypeText("test");

        string? changedText = null;
        _node.TextChanged.Subscribe(text => changedText = text);

        _node.Clear();

        Assert.Equal("", changedText);
    }

    [Fact]
    public void HandleInput_LeftArrow_MovesCursor()
    {
        TypeText("hello");

        // Move left
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));

        // Type at cursor position
        _node.HandleInput(new ConsoleKeyInfo('X', (ConsoleKey)0, false, false, false));

        Assert.Equal("helXlo", _node.Text);
    }

    [Fact]
    public void HandleInput_RightArrow_MovesCursor()
    {
        TypeText("hello");

        // Move to start
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));

        // Move right twice
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));

        // Type at cursor position
        _node.HandleInput(new ConsoleKeyInfo('X', (ConsoleKey)0, false, false, false));

        Assert.Equal("heXllo", _node.Text);
    }

    [Fact]
    public void HandleInput_RightArrow_SkipsWholeEmojiTextElement()
    {
        _node.HandlePaste(new PasteEvent("😀A"));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));

        _node.HandleInput(new ConsoleKeyInfo('X', (ConsoleKey)0, false, false, false));

        Assert.Equal("😀XA", _node.Text);
    }

    [Fact]
    public void HandleInput_Backspace_DeletesWholeEmojiTextElement()
    {
        _node.HandlePaste(new PasteEvent("😀A"));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));

        _node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("A", _node.Text);
    }

    [Fact]
    public void HandleInput_Home_MovesCursorToStart()
    {
        TypeText("hello");
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('X', (ConsoleKey)0, false, false, false));

        Assert.Equal("Xhello", _node.Text);
    }

    [Fact]
    public void HandleInput_End_MovesCursorToEnd()
    {
        TypeText("hello");
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('X', (ConsoleKey)0, false, false, false));

        Assert.Equal("helloX", _node.Text);
    }

    [Fact]
    public void HandleInput_Backspace_DeletesCharacter()
    {
        TypeText("hello");
        _node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("hell", _node.Text);
    }

    [Fact]
    public void HandleInput_Delete_DeletesCharacterAhead()
    {
        TypeText("hello");
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false));

        Assert.Equal("ello", _node.Text);
    }

    [Fact]
    public void HandleInput_Escape_ClearsText()
    {
        TypeText("hello");
        _node.HandleInput(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));

        Assert.Equal("", _node.Text);
    }

    #region History Tests

    [Fact]
    public void History_Disabled_UpArrow_ReturnsFalse()
    {
        // Default node has no history — Up should return false
        var handled = _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.False(handled);
    }

    [Fact]
    public void History_Disabled_DownArrow_ReturnsFalse()
    {
        var handled = _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Assert.False(handled);
    }

    [Fact]
    public void History_Enabled_UpArrow_ReturnsTrue()
    {
        using var node = new TextInputNode().WithHistory();
        var handled = node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.True(handled);
    }

    [Fact]
    public void History_Enabled_DownArrow_ReturnsTrue()
    {
        using var node = new TextInputNode().WithHistory();
        var handled = node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Assert.True(handled);
    }

    [Fact]
    public void History_RecallsLastSubmission()
    {
        using var node = new TextInputNode().WithHistory();

        TypeText(node, "first");
        PressEnter(node);
        node.Clear();

        TypeText(node, "second");
        PressEnter(node);
        node.Clear();

        // Press Up — should recall "second"
        PressUp(node);
        Assert.Equal("second", node.Text);
    }

    [Fact]
    public void History_NavigatesMultipleEntries()
    {
        using var node = new TextInputNode().WithHistory();

        TypeText(node, "first");
        PressEnter(node);
        node.Clear();

        TypeText(node, "second");
        PressEnter(node);
        node.Clear();

        TypeText(node, "third");
        PressEnter(node);
        node.Clear();

        // Navigate backward through all entries
        PressUp(node);
        Assert.Equal("third", node.Text);

        PressUp(node);
        Assert.Equal("second", node.Text);

        PressUp(node);
        Assert.Equal("first", node.Text);

        // At the beginning — stays on first
        PressUp(node);
        Assert.Equal("first", node.Text);
    }

    [Fact]
    public void History_DownRestoresSavedInput()
    {
        using var node = new TextInputNode().WithHistory();

        TypeText(node, "submitted");
        PressEnter(node);
        node.Clear();

        // Type partial text, then navigate history
        TypeText(node, "in-progress");
        PressUp(node);
        Assert.Equal("submitted", node.Text);

        // Press Down — should restore in-progress text
        PressDown(node);
        Assert.Equal("in-progress", node.Text);
    }

    [Fact]
    public void History_MaxEntries_EvictsOldest()
    {
        using var node = new TextInputNode().WithHistory(maxEntries: 2);

        TypeText(node, "first");
        PressEnter(node);
        node.Clear();

        TypeText(node, "second");
        PressEnter(node);
        node.Clear();

        TypeText(node, "third");
        PressEnter(node);
        node.Clear();

        // Should only have "second" and "third" (first was evicted)
        PressUp(node);
        Assert.Equal("third", node.Text);

        PressUp(node);
        Assert.Equal("second", node.Text);

        // At the beginning — stays on second (first was evicted)
        PressUp(node);
        Assert.Equal("second", node.Text);
    }

    [Fact]
    public void History_EmptySubmissionsNotAdded()
    {
        using var node = new TextInputNode().WithHistory();

        // Submit empty string
        PressEnter(node);
        node.Clear();

        TypeText(node, "real entry");
        PressEnter(node);
        node.Clear();

        // Up should recall "real entry" (empty was not added)
        PressUp(node);
        Assert.Equal("real entry", node.Text);

        // Up again — should stay on "real entry" (only one entry)
        PressUp(node);
        Assert.Equal("real entry", node.Text);
    }

    [Fact]
    public void History_EnterResetsHistoryIndex()
    {
        using var node = new TextInputNode().WithHistory();

        TypeText(node, "first");
        PressEnter(node);
        node.Clear();

        TypeText(node, "second");
        PressEnter(node);
        node.Clear();

        // Navigate to "first"
        PressUp(node);
        PressUp(node);
        Assert.Equal("first", node.Text);

        // Submit "first" again (Enter resets index)
        PressEnter(node);
        node.Clear();

        // Up should now recall the most recent entry ("first" — just submitted)
        PressUp(node);
        Assert.Equal("first", node.Text);
    }

    [Fact]
    public void AddHistory_AddsEntry()
    {
        using var node = new TextInputNode().WithHistory();

        node.AddHistory("programmatic entry");

        PressUp(node);
        Assert.Equal("programmatic entry", node.Text);
    }

    [Fact]
    public void AddHistory_NoOp_WhenDisabled()
    {
        // Default node — history disabled
        _node.AddHistory("should be ignored");

        // Up arrow still returns false (no history)
        var handled = _node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.False(handled);
    }

    [Fact]
    public void AddHistory_IgnoresEmptyStrings()
    {
        using var node = new TextInputNode().WithHistory();

        node.AddHistory("");
        node.AddHistory("real");

        PressUp(node);
        Assert.Equal("real", node.Text);

        // Only one entry
        PressUp(node);
        Assert.Equal("real", node.Text);
    }

    [Fact]
    public void AddHistory_RespectsMaxEntries()
    {
        using var node = new TextInputNode().WithHistory(maxEntries: 2);

        node.AddHistory("first");
        node.AddHistory("second");
        node.AddHistory("third");

        PressUp(node);
        Assert.Equal("third", node.Text);

        PressUp(node);
        Assert.Equal("second", node.Text);

        // first was evicted
        PressUp(node);
        Assert.Equal("second", node.Text);
    }

    [Fact]
    public void History_PasteContentRecalledAsCondensed()
    {
        using var node = new TextInputNode().WithHistory();

        // Simulate a paste
        node.HandlePaste(new Termina.Input.PasteEvent("line1\nline2\nline3"));
        PressEnter(node);
        node.Clear();

        // Recall — should show condensed form (Text setter handles multi-line)
        PressUp(node);
        Assert.Contains("[Pasted", node.Text);
    }

    #endregion

    private void TypeText(string text)
    {
        TypeText(_node, text);
    }

    private static void TypeText(TextInputNode node, string text)
    {
        foreach (var c in text)
        {
            node.HandleInput(new ConsoleKeyInfo(c, (ConsoleKey)0, false, false, false));
        }
    }

    private static void PressEnter(TextInputNode node)
    {
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
    }

    private static void PressUp(TextInputNode node)
    {
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
    }

    private static void PressDown(TextInputNode node)
    {
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
    }

    [Fact]
    public void ScrollOffset_ResetsToZero_AfterSubmitOrClear()
    {
        var scrollOffsetField = typeof(TextInputNode).GetField("_scrollOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(scrollOffsetField);
        
        TypeText(_node, "test");
        scrollOffsetField.SetValue(_node, 10);
        
        _node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        
        var scrollOffsetAfterSubmit = (int)scrollOffsetField.GetValue(_node)!;
        Assert.Equal(0, scrollOffsetAfterSubmit);
    }

    [Fact]
    public void ScrollOffset_ResetsToZero_AfterClear()
    {
        var scrollOffsetField = typeof(TextInputNode).GetField("_scrollOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(scrollOffsetField);
        
        TypeText(_node, "test");
        scrollOffsetField.SetValue(_node, 10);
        
        _node.Clear();
        
        var scrollOffsetAfterClear = (int)scrollOffsetField.GetValue(_node)!;
        Assert.Equal(0, scrollOffsetAfterClear);
    }
}

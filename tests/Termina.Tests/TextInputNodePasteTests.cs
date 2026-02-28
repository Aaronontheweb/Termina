// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using R3;

namespace Termina.Tests;

/// <summary>
/// Tests for bracketed paste mode support on <see cref="TextInputNode"/> and related infrastructure.
/// </summary>
public class TextInputNodePasteTests
{
    [Fact]
    public void HandlePaste_ShowsSummary_SingleLine()
    {
        var node = new TextInputNode();
        var result = node.HandlePaste(new PasteEvent("hello world"));
        Assert.True(result);
        // Single-line paste inserts inline (no summary)
        Assert.Equal("hello world", node.Text);
    }

    [Fact]
    public void HandlePaste_ShowsSummary_MultiLine()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("hello\nworld\nthird"));
        Assert.Equal("[Pasted 3 lines, 17 chars] ", node.Text);
    }

    [Fact]
    public void HandlePaste_PreservesNewlinesInContent()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("hello\r\nworld"));

        // Summary is shown, but Enter submits the full content with newlines preserved
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("hello\r\nworld", submitted);
    }

    [Fact]
    public void HandlePaste_DoesNotFireSubmitted()
    {
        var node = new TextInputNode();
        var submittedCount = 0;
        node.Submitted.Subscribe(_ => submittedCount++);

        node.HandlePaste(new PasteEvent("hello\nworld\nanother line"));

        Assert.Equal(0, submittedCount);
    }

    [Fact]
    public void HandlePaste_EnterSubmitsFullPasteContent()
    {
        var node = new TextInputNode();
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);

        var pasteContent = "line 1\nline 2\nline 3";
        node.HandlePaste(new PasteEvent(pasteContent));

        // Press Enter to submit
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal(pasteContent, submitted);
    }

    [Fact]
    public void HandlePaste_TypingAfterPaste_AccumulatesContent()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted content\nwith newlines"));

        // Type a character — appends to active text after paste segment
        node.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));

        // Display shows paste summary + typed char
        Assert.Equal("[Pasted 2 lines, 28 chars] a", node.Text);

        // Enter submits the full paste content + typed text
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("pasted content\nwith newlinesa", submitted);
    }

    [Fact]
    public void HandlePaste_BackspaceSingleLine_DeletesCharacter()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Single-line paste inserted inline — backspace deletes last char
        node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("pasted stuf", node.Text);
    }

    [Fact]
    public void HandlePaste_DeleteSingleLine_NoOpAtEnd()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Single-line paste inserted inline — cursor at end, delete is no-op
        var result = node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false));

        Assert.False(result);
        Assert.Equal("pasted stuff", node.Text);
    }

    [Fact]
    public void HandlePaste_EscapeClearsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Escape should clear everything
        node.HandleInput(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));

        Assert.Equal("", node.Text);
    }

    [Fact]
    public void HandlePaste_ClearMethodResetsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        node.Clear();

        Assert.Equal("", node.Text);

        // Enter after Clear submits empty, not paste content
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("", submitted);
    }

    [Fact]
    public void HandlePaste_FiresTextChanged()
    {
        var node = new TextInputNode();
        string? lastText = null;
        node.TextChanged.Subscribe(t => lastText = t);

        // Single-line paste emits the raw text (inserted inline)
        node.HandlePaste(new PasteEvent("test"));

        Assert.Equal("test", lastText);
    }

    [Fact]
    public void HandlePaste_ReturnsFalse_WhenContentIsEmpty()
    {
        var node = new TextInputNode();
        var result = node.HandlePaste(new PasteEvent(""));
        Assert.False(result);
        Assert.Equal("", node.Text);
    }

    [Fact]
    public void HandlePaste_SingleLine_InsertsAtCursorPosition()
    {
        var node = new TextInputNode();
        node.Text = "existing text";

        // Move cursor to position 8 ("existing|text" -> "existing| text")
        // We need to simulate pressing Home then Right 8 times, or just set directly
        // Instead, let's type some text, then paste in the middle
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        // Now cursor is at 0. Move right 8 times to after "existing"
        for (var i = 0; i < 8; i++)
            node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));

        node.HandlePaste(new PasteEvent(" new"));

        Assert.Equal("existing new text", node.Text);
    }

    [Fact]
    public void HandlePaste_TextPropertySetterClearsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted\nstuff"));

        // Setting Text property programmatically should clear committed segments
        node.Text = "manual text";

        Assert.Equal("manual text", node.Text);

        // Enter submits the manual text, not paste
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("manual text", submitted);
    }

    [Fact]
    public void TextSetter_MultiLineContent_ShowsCondensedSummary()
    {
        var node = new TextInputNode();

        // Simulate history recall of previously-pasted multi-line content
        node.Text = "line 1\nline 2\nline 3";

        Assert.Equal("[Pasted 3 lines, 20 chars] ", node.Text);
    }

    [Fact]
    public void TextSetter_MultiLineContent_SubmitsFullContent()
    {
        var node = new TextInputNode();
        var multiLine = "line 1\nline 2\nline 3";
        node.Text = multiLine;

        // Enter should submit the full multi-line content, not the summary
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal(multiLine, submitted);
    }

    [Fact]
    public void TextInputNode_ImplementsIPasteReceiver()
    {
        var node = new TextInputNode();
        Assert.IsAssignableFrom<IPasteReceiver>(node);
    }

    // === New tests for committed segments model ===

    [Fact]
    public void MultiLinePaste_ThenType_ThenSubmit_CombinesAllContent()
    {
        var node = new TextInputNode();

        // Paste multi-line content
        node.HandlePaste(new PasteEvent("line1\nline2"));
        // Type some text after
        node.HandleInput(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));

        // Submit should combine paste + typed
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("line1\nline2xy", submitted);
    }

    [Fact]
    public void MultiSegment_Paste_Type_Paste_Type_Submit()
    {
        var node = new TextInputNode();

        // Paste multi-line
        node.HandlePaste(new PasteEvent("a\nb"));
        // Type some
        node.HandleInput(new ConsoleKeyInfo('X', ConsoleKey.X, false, false, false));
        // Paste multi-line again — commits "X" as typed segment first
        node.HandlePaste(new PasteEvent("c\nd"));
        // Type more
        node.HandleInput(new ConsoleKeyInfo('Y', ConsoleKey.Y, false, false, false));

        // Submit should be all concatenated
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("a\nbXc\ndY", submitted);
    }

    [Fact]
    public void BackspaceAtPos0_PopsPasteSegment()
    {
        var node = new TextInputNode();

        // Paste multi-line (creates paste committed segment)
        node.HandlePaste(new PasteEvent("hello\nworld"));

        // Cursor is at 0, no active text. Backspace should pop the paste segment.
        node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("", node.Text);

        // Submit should yield nothing
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("", submitted);
    }

    [Fact]
    public void BackspaceAtPos0_MergesTypedSegmentBack()
    {
        var node = new TextInputNode();

        // Type text, then paste multi-line (which commits typed text as a segment)
        node.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false));
        node.HandlePaste(new PasteEvent("x\ny"));

        // Now we have: committed["ab" typed], committed[paste], _text=""
        // Backspace should pop the paste segment
        node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        // Now: committed["ab" typed], _text=""
        // Backspace again at pos 0 should merge "ab" back into _text
        node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("ab", node.Text);

        // Submit should yield "ab"
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("ab", submitted);
    }

    [Fact]
    public void EscapeClearsAllCommittedSegments()
    {
        var node = new TextInputNode();

        // Build up segments
        node.HandlePaste(new PasteEvent("a\nb"));
        node.HandleInput(new ConsoleKeyInfo('X', ConsoleKey.X, false, false, false));
        node.HandlePaste(new PasteEvent("c\nd"));
        node.HandleInput(new ConsoleKeyInfo('Y', ConsoleKey.Y, false, false, false));

        // Escape should clear everything
        node.HandleInput(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));

        Assert.Equal("", node.Text);

        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("", submitted);
    }

    [Fact]
    public void History_SavesFullCombinedContent()
    {
        var node = new TextInputNode().WithHistory();

        // Build up multi-segment input
        node.HandlePaste(new PasteEvent("hello\nworld"));
        node.HandleInput(new ConsoleKeyInfo('!', ConsoleKey.D1, false, false, false));

        // Submit (should record "hello\nworld!" to history)
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        node.Clear();

        // Navigate up in history
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));

        // The recalled content should be the multi-line + "!" — displayed as summary
        // When recalled via Text setter, multi-line shows as summary
        Assert.Contains("[Pasted", node.Text);

        // Submit should give back the original content
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("hello\nworld!", submitted);
    }

    [Fact]
    public void MultiLinePaste_WithExistingText_CommitsExistingFirst()
    {
        var node = new TextInputNode();

        // Type some text first
        node.HandleInput(new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false));

        // Paste multi-line — should commit "hi" as typed segment first
        node.HandlePaste(new PasteEvent("foo\nbar"));

        // Display should show typed prefix + paste summary
        Assert.Equal("hi[Pasted 2 lines, 7 chars] ", node.Text);

        // Submit should yield "hi" + "foo\nbar"
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("hifoo\nbar", submitted);
    }

    [Fact]
    public void MultiLinePaste_CursorInMiddle_SplitsText()
    {
        var node = new TextInputNode();

        // Type "abcd"
        node.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('c', ConsoleKey.C, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false));

        // Move cursor left 2 — cursor at position 2 (between b and c)
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));

        // Paste multi-line — should commit "ab" as typed, paste segment, _text = "cd"
        node.HandlePaste(new PasteEvent("x\ny"));

        // Submit should yield "ab" + "x\ny" + "cd"
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("abx\nycd", submitted);
    }

    [Fact]
    public void HandlePaste_EscapeAfterMultiLinePaste_ClearsAll()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("hello\nworld"));

        // Escape clears all committed segments + text
        node.HandleInput(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));

        Assert.Equal("", node.Text);
    }
}

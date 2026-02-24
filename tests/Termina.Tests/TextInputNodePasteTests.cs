// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;

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
        Assert.Equal("[Pasted 11 chars]", node.Text);
    }

    [Fact]
    public void HandlePaste_ShowsSummary_MultiLine()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("hello\nworld\nthird"));
        Assert.Equal("[Pasted 3 lines, 17 chars]", node.Text);
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
    public void HandlePaste_TypingClearsPaste_ReturnsToNormalEditing()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted content\nwith newlines"));

        // Type a character — should clear paste and start fresh
        node.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));

        Assert.Equal("a", node.Text);

        // Now Enter submits "a", not the paste content
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("a", submitted);
    }

    [Fact]
    public void HandlePaste_BackspaceClearsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Backspace should clear paste and return to empty
        node.HandleInput(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("", node.Text);
    }

    [Fact]
    public void HandlePaste_DeleteClearsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Delete should clear paste and return to empty
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false));

        Assert.Equal("", node.Text);
    }

    [Fact]
    public void HandlePaste_EscapeClearsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Escape should clear paste and return to empty
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

        node.HandlePaste(new PasteEvent("test"));

        // TextChanged gets the summary, not the raw content
        Assert.Equal("[Pasted 4 chars]", lastText);
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
    public void HandlePaste_ReplacesExistingText()
    {
        var node = new TextInputNode();
        node.Text = "existing text";

        node.HandlePaste(new PasteEvent("new paste"));

        Assert.Equal("[Pasted 9 chars]", node.Text);

        // Enter submits the paste content
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("new paste", submitted);
    }

    [Fact]
    public void HandlePaste_TextPropertySetterClearsPaste()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("pasted stuff"));

        // Setting Text property programmatically should clear paste
        node.Text = "manual text";

        Assert.Equal("manual text", node.Text);

        // Enter submits the manual text, not paste
        string? submitted = null;
        node.Submitted.Subscribe(t => submitted = t);
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("manual text", submitted);
    }

    [Fact]
    public void TextInputNode_ImplementsIPasteReceiver()
    {
        var node = new TextInputNode();
        Assert.IsAssignableFrom<IPasteReceiver>(node);
    }
}

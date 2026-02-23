// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using Termina.Terminal;

namespace Termina.Tests;

/// <summary>
/// Tests for bracketed paste mode support on <see cref="TextInputNode"/> and related infrastructure.
/// </summary>
public class TextInputNodePasteTests
{
    [Fact]
    public void HandlePaste_InsertsTextIntoInput()
    {
        var node = new TextInputNode();
        var result = node.HandlePaste(new PasteEvent("hello world"));
        Assert.True(result);
        Assert.Equal("hello world", node.Text);
    }

    [Fact]
    public void HandlePaste_SkipsNewlines_InPastedContent()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("hello\nworld"));
        Assert.Equal("helloworld", node.Text);
    }

    [Fact]
    public void HandlePaste_SkipsCarriageReturns_InPastedContent()
    {
        var node = new TextInputNode();
        node.HandlePaste(new PasteEvent("hello\r\nworld"));
        Assert.Equal("helloworld", node.Text);
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
    public void HandlePaste_FiresTextChanged()
    {
        var node = new TextInputNode();
        string? lastText = null;
        node.TextChanged.Subscribe(t => lastText = t);

        node.HandlePaste(new PasteEvent("test"));

        Assert.Equal("test", lastText);
    }

    [Fact]
    public void HandlePaste_RespectsMaxLength()
    {
        var node = new TextInputNode().WithMaxLength(5);
        node.HandlePaste(new PasteEvent("hello world"));
        Assert.Equal("hello", node.Text);
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
    public void HandlePaste_InsertsAtCursorPosition()
    {
        var node = new TextInputNode();
        // Pre-populate with "world", cursor at position 0
        node.Text = "world";
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));

        node.HandlePaste(new PasteEvent("hello "));
        Assert.Equal("hello world", node.Text);
    }

    [Fact]
    public void EnableBracketedPaste_HasCorrectValue()
    {
        Assert.Equal("\x1b[?2004h", AnsiCodes.EnableBracketedPaste);
    }

    [Fact]
    public void DisableBracketedPaste_HasCorrectValue()
    {
        Assert.Equal("\x1b[?2004l", AnsiCodes.DisableBracketedPaste);
    }

    [Fact]
    public void TextInputNode_ImplementsIPasteReceiver()
    {
        var node = new TextInputNode();
        Assert.IsAssignableFrom<IPasteReceiver>(node);
    }
}

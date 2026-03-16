// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Clipboard;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

public class CopyableTextNodeTests
{
    [Fact]
    public void Enter_CopiesFullContent()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "https://example.com");

        var handled = node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.True(handled);
        Assert.Equal("https://example.com", clipboard.LastCopiedText);
    }

    [Fact]
    public void NonEnterKey_IsNotHandled()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "abc");

        var handled = node.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));

        Assert.False(handled);
        Assert.Null(clipboard.LastCopiedText);
    }

    [Fact]
    public void ShiftArrow_SelectsText_AndEnterCopiesSelection()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "abcd");

        node.OnFocused();
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, true, false, false));
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, true, false, false));
        node.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Assert.Equal("ab", clipboard.LastCopiedText);
    }

    [Fact]
    public void CtrlA_SelectsAll_AndCtrlCCopiesSelection()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "select all");

        node.OnFocused();
        node.HandleInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, true));
        node.HandleInput(new ConsoleKeyInfo('\u0003', ConsoleKey.C, false, false, true));

        Assert.Equal("select all", clipboard.LastCopiedText);
    }

    [Fact]
    public void Escape_ClearsSelection()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "abcd");

        node.OnFocused();
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, true, false, false));
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, true, false, false));

        var handled = node.HandleInput(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false));

        Assert.True(handled);
        Assert.False(node.HasSelection);
    }

    [Fact]
    public void FocusChanges_RaiseInvalidation()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "abc");
        var invalidations = 0;
        using var subscription = node.Invalidated.Subscribe(_ => invalidations++);

        node.OnFocused();
        node.OnBlurred();

        Assert.Equal(2, invalidations);
    }

    [Fact]
    public void Render_WhenFocused_ShowsHint()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "copy me");
        var terminal = new VirtualTerminal(40, 4);
        var context = new RegionRenderContext(terminal, 0, 0, 40, 4);

        node.OnFocused();
        node.Render(context, new Rect(0, 0, 40, 4));

        Assert.Contains("copy me", terminal.ToString());
        Assert.Contains("Press Enter to copy", terminal.ToString());
    }

    [Fact]
    public void Render_WithSelection_HighlightsSelectedCharacters()
    {
        var clipboard = new TestClipboardService();
        var node = new CopyableTextNode(clipboard, "abcd");
        var terminal = new VirtualTerminal(10, 3);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 3);

        node.OnFocused();
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, true, false, false));
        node.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, true, false, false));
        node.Render(context, new Rect(0, 0, 10, 3));

        Assert.Equal(Color.BrightYellow, terminal.GetBackground(0, 0));
        Assert.Equal(Color.BrightYellow, terminal.GetBackground(1, 0));
        Assert.NotEqual(Color.BrightYellow, terminal.GetBackground(2, 0));
    }

    private sealed class TestClipboardService : IClipboardService
    {
        public string? LastCopiedText { get; private set; }

        public void Copy(string text)
        {
            LastCopiedText = text;
        }
    }
}

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

    private sealed class TestClipboardService : IClipboardService
    {
        public string? LastCopiedText { get; private set; }

        public void Copy(string text)
        {
            LastCopiedText = text;
        }
    }
}

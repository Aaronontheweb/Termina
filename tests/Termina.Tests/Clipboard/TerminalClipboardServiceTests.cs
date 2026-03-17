// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Clipboard;
using Termina.Notifications;

namespace Termina.Tests.Clipboard;

public class TerminalClipboardServiceTests
{
    [Fact]
    public void Copy_InvokesAllApplicableTransports_AndShowsToast()
    {
        var first = new TestTransport("first", canHandle: true, result: true);
        var second = new TestTransport("second", canHandle: true, result: true);
        var service = new TerminalClipboardService([first, second]);

        var copied = service.Copy("hello");

        Assert.True(copied);
        Assert.Equal(["hello"], first.CopiedTexts);
        Assert.Equal(["hello"], second.CopiedTexts);
    }

    [Fact]
    public void Copy_SkipsUnavailableTransport()
    {
        var skipped = new TestTransport("skip", canHandle: false, result: true);
        var used = new TestTransport("use", canHandle: true, result: true);
        var service = new TerminalClipboardService([skipped, used]);

        var copied = service.Copy("hello");

        Assert.True(copied);
        Assert.Empty(skipped.CopiedTexts);
        Assert.Equal(["hello"], used.CopiedTexts);
    }

    [Fact]
    public void Copy_ReturnsFalse_WhenNoTransportSucceeds()
    {
        var transport = new TestTransport("fail", canHandle: true, result: false);
        var service = new TerminalClipboardService([transport]);

        var copied = service.Copy("hello");

        Assert.False(copied);
    }

    private sealed class TestTransport : IClipboardTransport
    {
        private readonly bool _canHandle;
        private readonly bool _result;

        public TestTransport(string name, bool canHandle, bool result)
        {
            Name = name;
            _canHandle = canHandle;
            _result = result;
        }

        public string Name { get; }

        public List<string> CopiedTexts { get; } = [];

        public bool CanHandle() => _canHandle;

        public bool TryCopy(string text)
        {
            CopiedTexts.Add(text);
            return _result;
        }
    }
}

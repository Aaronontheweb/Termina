// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Platform;

namespace Termina.Tests.Input;

public class TerminalInputPipelineTests
{
    [Fact]
    public void Process_EmitsCurrentPublicEvents()
    {
        var pipeline = new TerminalInputPipeline();

        var events = pipeline.Process(Key('a'));

        var pressed = Assert.IsType<KeyPressed>(Assert.Single(events));
        Assert.Equal(ConsoleKey.A, pressed.KeyInfo.Key);
        Assert.Equal('a', pressed.KeyInfo.KeyChar);
    }

    [Fact]
    public void KittyReportAllKeysVisible_RoutesBareCsiArrowAsKey()
    {
        var pipeline = new TerminalInputPipeline(kittyReportAllKeysVisible: true);

        var events = FeedString(pipeline, "\x1b[A");

        var pressed = Assert.IsType<KeyPressed>(Assert.Single(events));
        Assert.Equal(ConsoleKey.UpArrow, pressed.KeyInfo.Key);
    }

    [Fact]
    public void KittyReportAllKeysNotVisible_RoutesBareCsiArrowAsKeyBeforeDeckmConfirmed()
    {
        var pipeline = new TerminalInputPipeline(kittyReportAllKeysVisible: false);

        var events = FeedString(pipeline, "\x1b[A");

        var press = Assert.IsType<KeyPressed>(Assert.Single(events));
        Assert.Equal(ConsoleKey.UpArrow, press.KeyInfo.Key);
    }

    [Fact]
    public void TerminalModeContext_RoutesBareCsiArrowAsKey()
    {
        var pipeline = new TerminalInputPipeline(new TerminalModeContext(KittyReportAllKeysVisible: true));

        var events = FeedString(pipeline, "\x1b[A");

        var pressed = Assert.IsType<KeyPressed>(Assert.Single(events));
        Assert.Equal(ConsoleKey.UpArrow, pressed.KeyInfo.Key);
    }

    [Fact]
    public void CheckEscapeTimeout_FlushesStandaloneEscape()
    {
        var tick = 0L;
        var pipeline = new TerminalInputPipeline(getTick: () => tick);

        Assert.Empty(pipeline.Process(Key('\x1b')));
        Assert.True(pipeline.IsBufferingEscape);

        tick += 60;
        var escape = pipeline.CheckEscapeTimeout();

        var pressed = Assert.IsType<KeyPressed>(escape);
        Assert.Equal(ConsoleKey.Escape, pressed.KeyInfo.Key);
        Assert.Equal('\x1b', pressed.KeyInfo.KeyChar);
        Assert.False(pipeline.IsBufferingEscape);
    }

    private static List<IInputEvent> FeedString(TerminalInputPipeline pipeline, string input)
    {
        var events = new List<IInputEvent>();
        foreach (var c in input)
            events.AddRange(pipeline.Process(Key(c)));

        return events;
    }

    private static ConsoleKeyInfo Key(char c) => c <= byte.MaxValue
        ? RawByteKeyMapper.ByteToKeyInfo((byte)c)
        : new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false);
}

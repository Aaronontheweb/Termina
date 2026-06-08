// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Platform;

namespace Termina.Tests.Input;

/// <summary>
/// Fixture-style characterization tests for the current terminal input decoding surface.
/// These tests intentionally assert today's public events so future decoder extraction can
/// prove behavior stayed compatible.
/// </summary>
public class TerminalInputFixtureTests
{
    public static IEnumerable<object[]> Fixtures()
    {
        yield return Case(
            "Legacy CSI PageUp",
            "\x1b[5~",
            Key(ConsoleKey.PageUp));

        yield return Case(
            "Legacy CSI Ctrl+PageUp",
            "\x1b[5;5~",
            Key(ConsoleKey.PageUp, modifiers: ConsoleModifiers.Control));

        yield return Case(
            "Legacy CSI Delete",
            "\x1b[3~",
            Key(ConsoleKey.Delete));

        yield return Case(
            "Unknown CSI tilde raw flush",
            "\x1b[9~",
            Key(ConsoleKey.Escape, '\x1b'),
            RawChar('['),
            RawChar('9'),
            RawChar('~'));

        yield return Case(
            "Bare CSI Up is keyboard arrow when DECCKM is not yet confirmed",
            "\x1b[A",
            Key(ConsoleKey.UpArrow));

        yield return ModeCase(
            "Bare CSI Up is key when kitty report-all-keys is visible",
            "\x1b[A",
            kittyReportAllKeysVisible: true,
            Key(ConsoleKey.UpArrow));

        yield return Case(
            "SS3 Up is key when kitty report-all-keys is not visible",
            "\x1bOA",
            Key(ConsoleKey.UpArrow));

        yield return ModeCase(
            "SS3 Up is alternate-scroll wheel when kitty report-all-keys is visible",
            "\x1bOA",
            kittyReportAllKeysVisible: true,
            Scroll(+1));

        yield return Case(
            "Kitty CSI-u Ctrl+Enter",
            "\x1b[13;5u",
            Key(ConsoleKey.Enter, '\r', ConsoleModifiers.Control));

        yield return Case(
            "Kitty CSI-u PUA F5",
            "\x1b[57368u",
            Key(ConsoleKey.F5));

        yield return ModeCase(
            "Kitty second-form Shift+Up",
            "\x1b[1;2A",
            kittyReportAllKeysVisible: true,
            Key(ConsoleKey.UpArrow, modifiers: ConsoleModifiers.Shift));

        yield return Case(
            "SGR mouse wheel up",
            "\x1b[<64;5;10M",
            Scroll(+1));

        yield return Case(
            "SGR mouse wheel down",
            "\x1b[<65;5;10M",
            Scroll(-1));

        yield return Case(
            "SGR mouse click is consumed",
            "\x1b[<0;5;10M");

        yield return Case(
            "Bracketed paste emits one paste event",
            "\x1b[200~hello world\x1b[201~",
            Paste("hello world"));

        yield return Case(
            "Bracketed paste preserves failed partial end sentinel",
            "\x1b[200~foo\x1b[20xbar\x1b[201~",
            Paste("foo\x1b[20xbar"));

        yield return Case(
            "Esc then non-CSI key flushes escape and processes key",
            "\x1bx",
            Key(ConsoleKey.Escape, '\x1b'),
            Key(ConsoleKey.X, 'x'));

        static object[] Case(
            string name,
            string input,
            params ExpectedEvent[] expectedEvents) =>
            [new ParserFixture(name, input, KittyReportAllKeysVisible: false, expectedEvents)];

        static object[] ModeCase(
            string name,
            string input,
            bool kittyReportAllKeysVisible,
            ExpectedEvent expectedEvent,
            params ExpectedEvent[] additionalExpectedEvents) =>
        [
            new ParserFixture(
                name,
                input,
                kittyReportAllKeysVisible,
                [expectedEvent, .. additionalExpectedEvents])
        ];
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void ParserFixtures_MatchCurrentPublicEvents(ParserFixture fixture)
    {
        var parser = new EscapeSequenceParser
        {
            KittyReportAllKeysVisible = fixture.KittyReportAllKeysVisible,
        };

        var events = FeedString(parser, fixture.Input);

        Assert.False(parser.IsBufferingEscape);
        Assert.Equal(fixture.ExpectedEvents.Count, events.Count);

        for (var i = 0; i < fixture.ExpectedEvents.Count; i++)
            AssertExpected(fixture.ExpectedEvents[i], events[i]);
    }

    [Fact]
    public void StandaloneEsc_EmitsEscapeAfterTimeout()
    {
        var tick = 0L;
        var parser = new EscapeSequenceParser(() => tick);

        var events = parser.Process(Key('\x1b'));
        Assert.Empty(events);
        Assert.True(parser.IsBufferingEscape);

        tick += 60;
        var escape = parser.CheckEscapeTimeout();

        var pressed = Assert.IsType<KeyPressed>(escape);
        Assert.Equal(ConsoleKey.Escape, pressed.KeyInfo.Key);
        Assert.Equal('\x1b', pressed.KeyInfo.KeyChar);
        Assert.False(parser.IsBufferingEscape);
    }

    private static List<IInputEvent> FeedString(EscapeSequenceParser parser, string input)
    {
        var events = new List<IInputEvent>();
        foreach (var c in input)
            events.AddRange(parser.Process(Key(c)));

        return events;
    }

    private static ConsoleKeyInfo Key(char c) => c <= byte.MaxValue
        ? RawByteKeyMapper.ByteToKeyInfo((byte)c)
        : new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false);

    private static void AssertExpected(ExpectedEvent expected, IInputEvent actual)
    {
        switch (expected.Kind)
        {
            case ExpectedEventKind.Key:
                var pressed = Assert.IsType<KeyPressed>(actual);
                Assert.Equal(expected.Key, pressed.KeyInfo.Key);
                Assert.Equal(expected.KeyChar, pressed.KeyInfo.KeyChar);
                Assert.Equal(expected.Modifiers, pressed.KeyInfo.Modifiers);
                break;

            case ExpectedEventKind.Scroll:
                var scroll = Assert.IsType<MouseScrollEvent>(actual);
                Assert.Equal(expected.Delta, scroll.Delta);
                break;

            case ExpectedEventKind.Paste:
                var paste = Assert.IsType<PasteEvent>(actual);
                Assert.Equal(expected.Content, paste.Content);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(expected), expected.Kind, "Unknown expected event kind.");
        }
    }

    private static ExpectedEvent Key(
        ConsoleKey key,
        char keyChar = '\0',
        ConsoleModifiers modifiers = default) =>
        new(ExpectedEventKind.Key, key, keyChar, modifiers);

    private static ExpectedEvent RawChar(char keyChar) =>
        new(ExpectedEventKind.Key, ConsoleKey.None, keyChar);

    private static ExpectedEvent Scroll(int delta) =>
        new(ExpectedEventKind.Scroll, Delta: delta);

    private static ExpectedEvent Paste(string content) =>
        new(ExpectedEventKind.Paste, Content: content);

    public sealed record ParserFixture(
        string Name,
        string Input,
        bool KittyReportAllKeysVisible,
        IReadOnlyList<ExpectedEvent> ExpectedEvents)
    {
        public override string ToString() => Name;
    }

    public sealed record ExpectedEvent(
        ExpectedEventKind Kind,
        ConsoleKey Key = ConsoleKey.None,
        char KeyChar = '\0',
        ConsoleModifiers Modifiers = default,
        int Delta = 0,
        string? Content = null);

    public enum ExpectedEventKind
    {
        Key,
        Scroll,
        Paste,
    }
}

// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Termina.Hosting;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Tests.Hosting;

public class TerminaRuntimeOptionsTests
{
    [Fact]
    public async Task RunAsync_DefaultsToLegacyMouseTracking()
    {
        var terminal = new VirtualTerminal();
        var app = CreateApp(terminal);
        app.RegisterRoute<TestPage, TestViewModel>("/");
        app.NavigateTo("/");

        using var cts = new CancellationTokenSource();
        var runTask = app.RunAsync(cts.Token);
        await WaitForConditionAsync(() => terminal.MouseEnabled);

        Assert.True(terminal.MouseEnabled);
        Assert.False(terminal.WheelScrollEnabled);

        cts.Cancel();
        await runTask;
    }

    [Fact]
    public async Task RunAsync_AlternateScrollWithoutRawInput_FallsBackToLegacyMouseTracking()
    {
        var terminal = new VirtualTerminal();
        var options = new TerminaRuntimeOptions
        {
            ScrollInputMode = ScrollInputMode.AlternateScroll,
        };

        var app = CreateApp(terminal, options);
        app.RegisterRoute<TestPage, TestViewModel>("/");
        app.NavigateTo("/");

        using var cts = new CancellationTokenSource();
        var runTask = app.RunAsync(cts.Token);
        await WaitForConditionAsync(() => terminal.MouseEnabled);

        Assert.True(terminal.MouseEnabled);
        Assert.False(terminal.WheelScrollEnabled);

        cts.Cancel();
        await runTask;
    }

    [Fact]
    public void ProcessEvent_DefaultCtrlCHandling_IgnoresCtrlCWhenRawInputInactive()
    {
        var app = CreateApp(new VirtualTerminal(), new TerminaRuntimeOptions());

        InvokeProcessEvent(app, new KeyPressed(new ConsoleKeyInfo('\x03', ConsoleKey.C, false, false, true)));

        Assert.Null(GetFirstCtrlCAt(app));
    }

    [Fact]
    public void ProcessEvent_DefaultCtrlCHandling_InterceptsCtrlCWhenRawInputActive()
    {
        var app = CreateApp(new VirtualTerminal(), new TerminaRuntimeOptions());
        SetRawInputActive(app, true);

        InvokeProcessEvent(app, new KeyPressed(new ConsoleKeyInfo('\x03', ConsoleKey.C, false, false, true)));

        Assert.NotNull(GetFirstCtrlCAt(app));
    }

    [Fact]
    public void EscapeSequenceParser_OnlyUsesKittyAlternateRoutingWhenReportAllKeysVisible()
    {
        var parser = new EscapeSequenceParser { KittyReportAllKeysVisible = false };
        var events = FeedString(parser, "\x1b[A");

        var scroll = Assert.Single(events);
        Assert.IsType<MouseScrollEvent>(scroll);

        parser = new EscapeSequenceParser { KittyReportAllKeysVisible = true };
        events = FeedString(parser, "\x1b[A");

        var key = Assert.Single(events);
        Assert.IsType<KeyPressed>(key);
    }

    private static TerminaApplication CreateApp(VirtualTerminal terminal, TerminaRuntimeOptions? options = null)
    {
        var services = new TestServiceProvider();
        return new TerminaApplication(terminal, options, services);
    }

    private static void InvokeProcessEvent(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEvent",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    private static DateTime? GetFirstCtrlCAt(TerminaApplication app)
    {
        var field = typeof(TerminaApplication).GetField(
            "_firstCtrlCAt",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);
        return (DateTime?)field!.GetValue(app);
    }

    private static void SetRawInputActive(TerminaApplication app, bool value)
    {
        var field = typeof(TerminaApplication).GetField(
            "_rawInputActive",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);
        field!.SetValue(app, value);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100; i++)
        {
            if (condition()) return;
            await Task.Delay(10);
        }

        Assert.Fail("Condition was not met within the expected time.");
    }

    private static List<IInputEvent> FeedString(EscapeSequenceParser parser, string s)
    {
        var all = new List<IInputEvent>();
        foreach (var c in s)
        {
            var info = c == '\x1b'
                ? new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)
                : new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false);
            all.AddRange(parser.Process(info));
        }

        return all;
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IEnumerable<IInputSource>))
                return Array.Empty<IInputSource>();

            return null;
        }
    }

    private sealed class TestPage : ReactivePage<TestViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("test");
    }

    private sealed class TestViewModel : ReactiveViewModel
    {
    }
}

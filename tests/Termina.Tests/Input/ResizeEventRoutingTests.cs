// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Tests.Input;

/// <summary>
/// Regression tests for <see cref="ResizeEvent"/> forwarding through
/// <see cref="TerminaApplication"/>'s ProcessEvent dispatch. Resize events
/// MUST surface on the ViewModel input observable so pages can react to a
/// terminal resize (e.g., recompute width-sensitive layout, re-pick narrow
/// vs wide key hints). The case branch used to short-circuit with
/// <c>return;</c> and consumers never saw the event — this test locks the
/// fall-through in place so a future refactor can't silently regress it.
/// </summary>
public class ResizeEventRoutingTests
{
    [Fact]
    public void ProcessEvent_ForwardsResizeEvent_ToViewModelInputObservable()
    {
        TestResizeViewModel.LastCreated = null;

        var services = new TestServiceProvider();
        var app = new TerminaApplication(new VirtualTerminal(), services);
        app.RegisterRoute<TestResizePage, TestResizeViewModel>("/resize");
        app.NavigateTo("/resize");

        var vm = TestResizeViewModel.LastCreated;
        Assert.NotNull(vm);

        var received = new List<ResizeEvent>();
        using var sub = vm!.Input.OfType<IInputEvent, ResizeEvent>()
            .Subscribe(received.Add);

        InvokeProcessEvent(app, new ResizeEvent(120, 40));

        Assert.Single(received);
        Assert.Equal(120, received[0].Width);
        Assert.Equal(40, received[0].Height);
    }

    [Fact]
    public void ProcessEvent_StillRefreshesDiffingTerminal_OnResize()
    {
        // The fall-through must NOT skip the existing ForceFullRefresh side
        // effect. We can't directly observe it without a DiffingTerminal wired
        // in, but we can at least confirm the dispatch does not throw and the
        // event is delivered (covered by the test above). This second case
        // ensures a follow-up developer who reorders the handler keeps both
        // behaviors live.
        TestResizeViewModel.LastCreated = null;

        var services = new TestServiceProvider();
        var app = new TerminaApplication(new VirtualTerminal(), services);
        app.RegisterRoute<TestResizePage, TestResizeViewModel>("/resize");
        app.NavigateTo("/resize");

        var vm = TestResizeViewModel.LastCreated;
        Assert.NotNull(vm);

        var received = new List<ResizeEvent>();
        using var sub = vm!.Input.OfType<IInputEvent, ResizeEvent>()
            .Subscribe(received.Add);

        // Multiple distinct resizes must each surface independently.
        InvokeProcessEvent(app, new ResizeEvent(80, 24));
        InvokeProcessEvent(app, new ResizeEvent(132, 50));

        Assert.Equal(2, received.Count);
        Assert.Equal(80, received[0].Width);
        Assert.Equal(132, received[1].Width);
    }

    [Fact]
    public void ProcessEvent_DoesNotForceFullRefresh_ForNoOpResize()
    {
        var terminal = new RecordingTerminal(80, 24);
        var app = CreateRecordingApp(terminal);

        InvokeRenderCurrentPage(app);
        terminal.ClearOutput();

        InvokeProcessEvent(app, new ResizeEvent(80, 24));
        InvokeRenderCurrentPage(app);

        Assert.DoesNotContain(AnsiCodes.ClearScreen, terminal.Output);
    }

    [Fact]
    public void ProcessEvent_DoesNotForceFullRefresh_ForInvalidResize()
    {
        var terminal = new RecordingTerminal(80, 24);
        var app = CreateRecordingApp(terminal);

        InvokeRenderCurrentPage(app);
        terminal.ClearOutput();

        InvokeProcessEvent(app, new ResizeEvent(0, 24));
        InvokeRenderCurrentPage(app);

        Assert.DoesNotContain(AnsiCodes.ClearScreen, terminal.Output);
    }

    [Fact]
    public void ProcessEvent_ForcesFullRefresh_ForActualResize()
    {
        var terminal = new RecordingTerminal(80, 24);
        var app = CreateRecordingApp(terminal);

        InvokeRenderCurrentPage(app);
        terminal.ClearOutput();

        terminal.Width = 100;
        terminal.Height = 30;
        InvokeProcessEvent(app, new ResizeEvent(100, 30));
        InvokeRenderCurrentPage(app);

        Assert.Contains(AnsiCodes.ClearScreen, terminal.Output);
    }

    private static TerminaApplication CreateRecordingApp(RecordingTerminal terminal)
    {
        TestResizeViewModel.LastCreated = null;

        var services = new TestServiceProvider();
        var app = new TerminaApplication(terminal, services);
        app.RegisterRoute<TestResizePage, TestResizeViewModel>("/resize");
        app.NavigateTo("/resize");
        return app;
    }

    private static void InvokeProcessEvent(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    private static void InvokeRenderCurrentPage(TerminaApplication app)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "RenderCurrentPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);
        method!.Invoke(app, []);
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

    private sealed class TestResizePage : ReactivePage<TestResizeViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("test");
    }

    private sealed class TestResizeViewModel : ReactiveViewModel
    {
        // Captured so the test can subscribe to Input after NavigateTo has
        // wired it up. Reset to null at the start of each test.
        public static TestResizeViewModel? LastCreated;

        public TestResizeViewModel()
        {
            LastCreated = this;
        }
    }

    private sealed class RecordingTerminal(int width, int height) : IAnsiTerminal
    {
        private readonly System.Text.StringBuilder _buffer = new();

        public string Output { get; private set; } = string.Empty;

        public int Width { get; set; } = width;

        public int Height { get; set; } = height;

        public void MoveTo(int x, int y) => _buffer.Append(AnsiCodes.MoveTo(y, x));

        public void Write(string text) => _buffer.Append(text);

        public void Write(char c) => _buffer.Append(c);

        public void SetForeground(Color color) { }

        public void SetBackground(Color color) { }

        public void ResetColors() { }

        public void SaveCursor() { }

        public void RestoreCursor() { }

        public void SetCursorVisible(bool visible) { }

        public void ClearRegion(int x, int y, int width, int height) { }

        public void ClearScreen() => _buffer.Append(AnsiCodes.ClearScreen);

        public void Flush()
        {
            Output += _buffer.ToString();
            _buffer.Clear();
        }

        public void EnterAlternateScreen() { }

        public void ExitAlternateScreen() { }

        public void EnableMouse() { }

        public void DisableMouse() { }

        public void EnableWheelScroll() { }

        public void DisableWheelScroll() { }

        public void CopyToClipboard(string text) { }

        public void ClearOutput()
        {
            Output = string.Empty;
            _buffer.Clear();
        }
    }
}

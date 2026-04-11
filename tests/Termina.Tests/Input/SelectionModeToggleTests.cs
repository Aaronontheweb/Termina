// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Tests.Input;

/// <summary>
/// Tests for the framework-level selection mode toggle in <see cref="TerminaApplication"/>.
/// Selection mode hands mouse input back to the terminal emulator by calling
/// <see cref="IAnsiTerminal.DisableMouse"/>, allowing native click-drag selection.
/// </summary>
public class SelectionModeToggleTests
{
    [Fact]
    public void EnterSelectionMode_DisablesTerminalMouseTracking()
    {
        var terminal = new VirtualTerminal();
        terminal.EnableMouse();
        Assert.True(terminal.MouseEnabled);

        var app = NewApp(terminal);
        app.EnterSelectionMode();

        Assert.False(terminal.MouseEnabled);
        Assert.True(app.IsSelectionModeActive);
    }

    [Fact]
    public void ExitSelectionMode_ReenablesTerminalMouseTracking()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);

        app.EnterSelectionMode();
        app.ExitSelectionMode();

        Assert.True(terminal.MouseEnabled);
        Assert.False(app.IsSelectionModeActive);
    }

    [Fact]
    public void EnterSelectionMode_IsIdempotent()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);

        app.EnterSelectionMode();
        app.EnterSelectionMode();

        Assert.True(app.IsSelectionModeActive);
        Assert.False(terminal.MouseEnabled);
    }

    [Fact]
    public void ExitSelectionMode_IsIdempotent()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);

        app.ExitSelectionMode();

        Assert.False(app.IsSelectionModeActive);
    }

    [Fact]
    public void F6_Toggles_SelectionMode_On_Then_Off()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);
        RegisterMinimalPage(app);

        ProcessKey(app, ConsoleKey.F6);
        Assert.True(app.IsSelectionModeActive);
        Assert.False(terminal.MouseEnabled);

        ProcessKey(app, ConsoleKey.F6);
        Assert.False(app.IsSelectionModeActive);
        Assert.True(terminal.MouseEnabled);
    }

    [Fact]
    public void Escape_ExitsSelectionMode_And_IsSwallowed()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);
        var page = RegisterMinimalPage(app);

        app.EnterSelectionMode();
        page.EscapeCount = 0;

        ProcessKey(app, ConsoleKey.Escape);

        Assert.False(app.IsSelectionModeActive);
        Assert.Equal(0, page.EscapeCount);
    }

    [Fact]
    public void Escape_OutsideSelectionMode_ReachesPage()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);
        var page = RegisterMinimalPage(app);

        page.EscapeCount = 0;
        ProcessKey(app, ConsoleKey.Escape);

        Assert.Equal(1, page.EscapeCount);
    }

    [Fact]
    public void F6_WithModifier_IsNotIntercepted()
    {
        var terminal = new VirtualTerminal();
        var app = NewApp(terminal);
        RegisterMinimalPage(app);

        // Shift+F6 should NOT toggle selection mode — only bare F6.
        InvokeProcessEvent(app, new KeyPressed(
            new ConsoleKeyInfo('\0', ConsoleKey.F6, shift: true, alt: false, control: false)));

        Assert.False(app.IsSelectionModeActive);
    }

    private static TerminaApplication NewApp(VirtualTerminal terminal) => new(terminal);

    private static TestPage RegisterMinimalPage(TerminaApplication app)
    {
        app.RegisterRoute<TestPage, TestViewModel>("/test");
        app.NavigateTo("/test");
        return GetActivePage(app);
    }

    private static TestPage GetActivePage(TerminaApplication app)
    {
        var field = typeof(TerminaApplication).GetField("_currentPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        var page = field!.GetValue(app);
        return Assert.IsType<TestPage>(page);
    }

    private static void ProcessKey(TerminaApplication app, ConsoleKey key)
    {
        InvokeProcessEvent(app, new KeyPressed(new ConsoleKeyInfo('\0', key, false, false, false)));
    }

    private static void InvokeProcessEvent(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    public sealed class TestPage : ReactivePage<TestViewModel>
    {
        public int EscapeCount { get; set; }

        public override ILayoutNode BuildLayout() => new TextNode("test");

        public override bool HandlePageInput(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Key == ConsoleKey.Escape)
            {
                EscapeCount++;
                return true;
            }
            return base.HandlePageInput(keyInfo);
        }
    }

    public sealed class TestViewModel : ReactiveViewModel
    {
    }
}

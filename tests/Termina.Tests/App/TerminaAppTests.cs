// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.App;
using Termina.Input;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.App;

/// <summary>
/// Integration tests for TerminaApp.
/// </summary>
public class TerminaAppTests
{
    [Fact]
    public async Task CreateHeadless_CreatesWorkingApp()
    {
        await using var app = TerminaApp.CreateHeadless(80, 24);

        Assert.Equal(80, app.Width);
        Assert.Equal(24, app.Height);
        Assert.False(app.IsRunning);
    }

    [Fact]
    public async Task RegisterRegion_AddsToRenderer()
    {
        await using var app = TerminaApp.CreateHeadless();
        var region = Region.FullScreen("main");

        app.RegisterRegion(region);

        Assert.NotNull(app.Renderer.GetRegion("main"));
    }

    [Fact]
    public async Task RegisterFocusableRegion_AddsToFocusManager()
    {
        await using var app = TerminaApp.CreateHeadless();
        var region = new Region("input",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(20),
            new LayoutConstraint.Fixed(1)) { Focusable = true };

        app.RegisterRegion(region);

        Assert.Contains(region, app.Focus.FocusableRegions);
    }

    [Fact]
    public async Task RunAsync_ProcessesKeyEvents()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = TerminaApp.CreateHeadless(terminal, input);

        var keysReceived = new List<ConsoleKey>();
        app.OnKeyPressed += key =>
        {
            keysReceived.Add(key.Key);
            if (key.Key == ConsoleKey.Escape)
                app.Stop();
            return true;
        };

        // Queue input then stop
        input.EnqueueKey(ConsoleKey.A);
        input.EnqueueKey(ConsoleKey.B);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.Contains(ConsoleKey.A, keysReceived);
        Assert.Contains(ConsoleKey.B, keysReceived);
        Assert.Contains(ConsoleKey.Escape, keysReceived);
    }

    [Fact]
    public async Task RenderRegion_UpdatesTerminal()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = TerminaApp.CreateHeadless(terminal, input);

        var region = Region.FullScreen("main");
        app.RegisterRegion(region);

        app.OnKeyPressed += k =>
        {
            if (k.Key == ConsoleKey.Escape)
            {
                app.Stop();
                return true;
            }
            return false;
        };

        // Queue a key to trigger render, then content, then exit
        input.EnqueueKey(ConsoleKey.A); // Trigger to allow render
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        // Start running (this will process events)
        var runTask = app.RunAsync();

        // Give it a moment to start then render
        await Task.Delay(50);
        app.Renderer.RenderRegion("main", new Text("Hello World"));

        await runTask;

        Assert.True(terminal.Contains("Hello World"));
    }

    [Fact]
    public async Task TextInput_HandlesKeyboardInput()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = TerminaApp.CreateHeadless(terminal, input);

        var region = new Region("input",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(40),
            new LayoutConstraint.Fixed(1)) { Focusable = true };
        app.RegisterRegion(region);

        var textInput = new TextInput { IsFocused = true };
        string? submittedText = null;
        textInput.OnSubmit += text => submittedText = text;

        app.OnKeyPressed += key =>
        {
            if (key.Key == ConsoleKey.Escape)
            {
                app.Stop();
                return true;
            }
            if (textInput.HandleKey(key))
            {
                app.Renderer.RenderRegion("input", textInput);
                return true;
            }
            return false;
        };

        // Type and submit
        input.EnqueueString("Test message");
        input.EnqueueKey(ConsoleKey.Enter);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.Equal("Test message", submittedText);
    }

    [Fact]
    public async Task Panel_RendersWithBorder()
    {
        var terminal = new VirtualTerminal(40, 10);
        var input = new VirtualInputSource();
        await using var app = TerminaApp.CreateHeadless(terminal, input);

        var region = Region.FullScreen("main");
        app.RegisterRegion(region);

        var panel = new Panel
        {
            Title = "Test",
            Border = BorderStyle.Single,
            Content = new Text("Content")
        };

        app.Renderer.RenderRegion("main", panel);

        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();
        app.OnKeyPressed += k => { app.Stop(); return true; };

        await app.RunAsync();

        Assert.True(terminal.Contains("Test"));
        Assert.True(terminal.Contains("Content"));
        Assert.Equal('┌', terminal.GetChar(0, 0)); // Top-left corner
    }

    [Fact]
    public async Task FocusManager_TabCyclesFocus()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = TerminaApp.CreateHeadless(terminal, input);

        var region1 = new Region("r1",
            new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(1))
            { Focusable = true, TabOrder = 0 };
        var region2 = new Region("r2",
            new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(1),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(1))
            { Focusable = true, TabOrder = 1 };

        app.RegisterRegion(region1);
        app.RegisterRegion(region2);
        app.Focus.FocusFirst();

        var focusChanges = new List<string?>();
        app.Focus.OnFocusChanged += (old, @new) => focusChanges.Add(@new?.Id);

        // Tab should cycle focus
        input.EnqueueKey(ConsoleKey.Tab);
        input.EnqueueKey(ConsoleKey.Tab);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        app.OnKeyPressed += key =>
        {
            if (key.Key == ConsoleKey.Escape)
            {
                app.Stop();
                return true;
            }
            return false; // Let default Tab handling work
        };

        await app.RunAsync();

        Assert.Contains("r2", focusChanges); // Tabbed to r2
        Assert.Contains("r1", focusChanges); // Wrapped back to r1
    }

    [Fact]
    public async Task MultipleRegions_RenderIndependently()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = TerminaApp.CreateHeadless(terminal, input);

        var topRegion = Region.TopRow("top", 3);
        var bottomRegion = Region.BottomRow("bottom", 1);

        app.RegisterRegion(topRegion);
        app.RegisterRegion(bottomRegion);

        app.Renderer.RenderRegion("top", new Panel
        {
            Border = BorderStyle.Double,
            Content = new Text("Header")
        });
        app.Renderer.RenderRegion("bottom", new Text("Status bar"));

        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();
        app.OnKeyPressed += k => { app.Stop(); return true; };

        await app.RunAsync();

        Assert.True(terminal.Contains("Header"));
        Assert.True(terminal.Contains("Status bar"));

        // Verify positions - header at top, status at bottom
        Assert.Equal('╔', terminal.GetChar(0, 0)); // Double border at top
    }
}

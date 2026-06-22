// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Threading.Channels;
using R3;
using Termina.Input;
using Termina.Layout;
using Termina.Navigation;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Tests;

/// <summary>
/// Navigation initiated by a ViewModel or page must be posted to the event
/// channel and processed on the render-loop thread — never run synchronously on
/// the caller's thread. A ViewModel commonly calls <c>Navigate(...)</c> from an
/// async continuation on the thread pool; running navigation there mutates
/// <c>_currentPage</c> concurrently with the render loop and publishes a
/// not-yet-bound page, which the render loop then renders before <c>OnBound()</c>
/// has run. Under ARM64's weak memory model that surfaces as a
/// <see cref="NullReferenceException" /> in the page's <c>BuildLayout()</c>
/// (netclaw#1069 — <c>netclaw init</c> crash on Apple Silicon).
/// </summary>
public class NavigationThreadSafetyTests
{
    [Fact]
    public void ViewModelInitiatedNavigation_IsPostedToEventChannel_NotRunSynchronously()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        app.RegisterRoute<NavigatingPage, NavigatingViewModel>("/page-a");
        app.RegisterRoute<PlainPage, IdleViewModel>("/page-b");

        // Initial navigation runs synchronously (no render loop yet). The page-a
        // ViewModel requests navigation to /page-b from OnActivated().
        app.NavigateTo("/page-a");

        // The ViewModel's request must NOT have navigated synchronously.
        Assert.Equal("/page-a", app.CurrentPath);

        // It must have been posted to the event channel as a NavigationRequested.
        var navRequest = ReadQueuedNavigation(app);
        Assert.Equal("/page-b", navRequest.PageKey);

        // Processing the event — what the render-loop thread does — performs the
        // navigation, fully binding the page before it can be rendered.
        InvokeProcessEvent(app, navRequest);
        Assert.Equal("/page-b", app.CurrentPath);
    }

    [Fact]
    public void PageInitiatedNavigation_IsPostedToEventChannel_NotRunSynchronously()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        app.RegisterRoute<PageNavigatingPage, IdleViewModel>("/start");
        app.RegisterRoute<PlainPage, IdleViewModel>("/dest");

        // The page requests navigation to /dest from its OnNavigatedTo() override.
        app.NavigateTo("/start");

        Assert.Equal("/start", app.CurrentPath);

        var navRequest = ReadQueuedNavigation(app);
        Assert.Equal("/dest", navRequest.PageKey);

        InvokeProcessEvent(app, navRequest);
        Assert.Equal("/dest", app.CurrentPath);
    }

    [Fact]
    public void ProcessEventAndMaybeRender_SkipsRender_WhenInputQueuesNavigation()
    {
        var terminal = new VirtualTerminal();
        var app = new TerminaApplication(terminal);
        app.RegisterRoute<InputNavigatingPage, InputNavigatingViewModel>("/start");
        app.RegisterRoute<PlainPage, IdleViewModel>("/dest");
        app.NavigateTo("/start");
        InvokeRenderCurrentPage(app);
        var outputCountBeforeInput = terminal.RawOutput.Count;

        InvokeProcessEventAndMaybeRender(
            app,
            new KeyPressed(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));

        Assert.Equal("/start", app.CurrentPath);
        Assert.Equal(outputCountBeforeInput, terminal.RawOutput.Count);

        var navRequest = ReadQueuedNavigation(app);
        Assert.Equal("/dest", navRequest.PageKey);

        InvokeProcessEventAndMaybeRender(app, navRequest);

        Assert.Equal("/dest", app.CurrentPath);
        Assert.True(terminal.RawOutput.Count > outputCountBeforeInput);
        Assert.Contains("plain", terminal.ToString());
    }

    [Fact]
    public void ProcessEventAndMaybeRender_SkipsIntermediateRender_WhenNavigationQueuesNavigation()
    {
        var terminal = new VirtualTerminal();
        var app = new TerminaApplication(terminal);
        app.RegisterRoute<InputNavigatingToMiddlePage, InputNavigatingToMiddleViewModel>("/start");
        app.RegisterRoute<RedirectingPage, RedirectingViewModel>("/middle");
        app.RegisterRoute<FinalPage, IdleViewModel>("/final");
        app.NavigateTo("/start");
        InvokeRenderCurrentPage(app);
        var outputCountBeforeInput = terminal.RawOutput.Count;

        InvokeProcessEventAndMaybeRender(
            app,
            new KeyPressed(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));
        Assert.Equal(outputCountBeforeInput, terminal.RawOutput.Count);

        var middleNavRequest = ReadQueuedNavigation(app);
        Assert.Equal("/middle", middleNavRequest.PageKey);

        InvokeProcessEventAndMaybeRender(app, middleNavRequest);

        Assert.Equal("/middle", app.CurrentPath);
        Assert.Equal(outputCountBeforeInput, terminal.RawOutput.Count);

        var finalNavRequest = ReadQueuedNavigation(app);
        Assert.Equal("/final", finalNavRequest.PageKey);

        InvokeProcessEventAndMaybeRender(app, finalNavRequest);

        Assert.Equal("/final", app.CurrentPath);
        Assert.True(terminal.RawOutput.Count > outputCountBeforeInput);
        Assert.Contains("final", terminal.ToString());
    }

    private static NavigationRequested ReadQueuedNavigation(TerminaApplication app)
    {
        var field = typeof(TerminaApplication).GetField(
            "_eventChannel", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);

        var channel = (Channel<object>)field!.GetValue(app)!;
        while (channel.Reader.TryRead(out var evt))
        {
            if (evt is NavigationRequested navRequest)
                return navRequest;
        }

        Assert.Fail("Expected a NavigationRequested event on the channel.");
        return null!;
    }

    private static void InvokeProcessEvent(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    private static void InvokeProcessEventAndMaybeRender(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEventAndMaybeRender", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    private static void InvokeRenderCurrentPage(TerminaApplication app)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "RenderCurrentPage", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(app, []);
    }

    private sealed class NavigatingViewModel : ReactiveViewModel
    {
        public override void OnActivated()
        {
            base.OnActivated();
            Navigate("/page-b");
        }
    }

    private sealed class IdleViewModel : ReactiveViewModel
    {
    }

    private sealed class InputNavigatingViewModel : ReactiveViewModel
    {
        public override void OnActivated()
        {
            base.OnActivated();
            Input.OfType<IInputEvent, KeyPressed>()
                .Subscribe(key =>
                {
                    if (key.KeyInfo.Key == ConsoleKey.Enter)
                        Navigate("/dest");
                })
                .DisposeWith(Subscriptions);
        }
    }

    private sealed class InputNavigatingToMiddleViewModel : ReactiveViewModel
    {
        public override void OnActivated()
        {
            base.OnActivated();
            Input.OfType<IInputEvent, KeyPressed>()
                .Subscribe(key =>
                {
                    if (key.KeyInfo.Key == ConsoleKey.Enter)
                        Navigate("/middle");
                })
                .DisposeWith(Subscriptions);
        }
    }

    private sealed class RedirectingViewModel : ReactiveViewModel
    {
        public override void OnActivated()
        {
            base.OnActivated();
            Navigate("/final");
        }
    }

    private sealed class NavigatingPage : ReactivePage<NavigatingViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("A");
    }

    private sealed class InputNavigatingPage : ReactivePage<InputNavigatingViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("start");
    }

    private sealed class InputNavigatingToMiddlePage : ReactivePage<InputNavigatingToMiddleViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("start");
    }

    private sealed class RedirectingPage : ReactivePage<RedirectingViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("middle");
    }

    private sealed class FinalPage : ReactivePage<IdleViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("final");
    }

    private sealed class PageNavigatingPage : ReactivePage<IdleViewModel>
    {
        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            Navigate("/dest");
        }

        public override ILayoutNode BuildLayout() => new TextNode("start");
    }

    private sealed class PlainPage : ReactivePage<IdleViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("plain");
    }
}

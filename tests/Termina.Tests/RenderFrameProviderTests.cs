// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;
using R3;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Tests;

public class RenderFrameProviderTests
{
    [Fact]
    public void Post_QueuesActionUntilEventIsProcessed()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        var ran = false;

        app.Post(() => ran = true);

        Assert.False(ran);
        ProcessNextEvent(app);
        Assert.True(ran);
    }

    [Fact]
    public void InvokeAsync_CompletesAfterQueuedActionRuns()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        var ran = false;

        var task = app.InvokeAsync(() => ran = true);

        Assert.False(task.IsCompleted);
        ProcessNextEvent(app);

        Assert.True(ran);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task InvokeAsync_FaultsTask_WhenActionThrows()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        var task = app.InvokeAsync(() => throw new InvalidOperationException("boom"));

        ProcessNextEvent(app);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task InvokeAsync_CancelledBeforeProcessing_DoesNotRunAction()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        using var cts = new CancellationTokenSource();
        var ran = false;

        var task = app.InvokeAsync(() => ran = true, cts.Token);
        await cts.CancelAsync();

        ProcessNextEvent(app);

        Assert.False(ran);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public void ObserveOnRenderFrameProvider_DeliversOnlyWhenFrameEventIsProcessed()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        var subject = new Subject<int>();
        var observed = new List<int>();
        using var subscription = subject
            .ObserveOn(app.RenderFrameProvider)
            .Subscribe(observed.Add);

        subject.OnNext(42);

        Assert.Empty(observed);
        ProcessNextEvent(app);

        Assert.Equal([42], observed);
        Assert.Equal(1, app.RenderFrameProvider.GetFrameCount());
    }

    [Fact]
    public void ActiveFrameWork_IsPacedByRenderFrameInterval()
    {
        var timeProvider = new FakeTimeProvider();
        var frameRequests = 0;
        using var provider = new TerminaRenderFrameProvider(
            () => frameRequests++,
            timeProvider,
            TimeSpan.FromMilliseconds(10));
        var workItem = new ContinueOnceFrameWorkItem();

        provider.Register(workItem);

        Assert.Equal(1, frameRequests);

        provider.AdvanceFrame();

        Assert.Equal(1, frameRequests);
        Assert.Equal(1, workItem.MoveNextCount);

        timeProvider.Advance(TimeSpan.FromMilliseconds(9));
        Assert.Equal(1, frameRequests);

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, frameRequests);

        provider.AdvanceFrame();

        Assert.Equal(2, workItem.MoveNextCount);
    }

    [Fact]
    public void AsLayout_WithFrameProvider_MarshalsInvalidationToFrame()
    {
        var app = new TerminaApplication(new VirtualTerminal());
        var subject = new Subject<int>();
        var node = subject.AsLayout(
            value => new TextNode(value.ToString()),
            app.RenderFrameProvider);
        var invalidations = 0;
        using var subscription = node.Invalidated.Subscribe(_ => invalidations++);

        subject.OnNext(1);

        Assert.Equal(0, invalidations);
        ProcessNextEvent(app);

        Assert.Equal(1, invalidations);
    }

    [Fact]
    public void SpinnerNode_WithFrameProvider_MarshalsTimerInvalidationToFrame()
    {
        var timeProvider = new FakeTimeProvider();
        var frameRequests = 0;
        using var frameProvider = new TerminaRenderFrameProvider(
            () => frameRequests++,
            timeProvider,
            TimeSpan.FromMilliseconds(10));
        using var spinner = new SpinnerNode(
            intervalMs: 80,
            timeProvider: timeProvider,
            frameProvider: frameProvider);
        spinner.OnActivate();
        var invalidations = 0;
        using var subscription = spinner.Invalidated.Subscribe(_ => invalidations++);

        timeProvider.Advance(TimeSpan.FromMilliseconds(80));

        Assert.Equal(0, invalidations);
        Assert.Equal(1, frameRequests);

        frameProvider.AdvanceFrame();

        Assert.Equal(1, invalidations);
    }

    private static object ProcessNextEvent(TerminaApplication app)
    {
        var channel = GetEventChannel(app);
        Assert.True(channel.Reader.TryRead(out var evt), "Expected an event to be queued.");
        InvokeProcessEvent(app, evt);
        return evt;
    }

    private static Channel<object> GetEventChannel(TerminaApplication app)
    {
        var field = typeof(TerminaApplication).GetField(
            "_eventChannel", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (Channel<object>)field!.GetValue(app)!;
    }

    private static void InvokeProcessEvent(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    private sealed class ContinueOnceFrameWorkItem : IFrameRunnerWorkItem
    {
        public int MoveNextCount { get; private set; }

        public bool MoveNext(long frameCount)
        {
            MoveNextCount++;
            return MoveNextCount == 1;
        }
    }
}

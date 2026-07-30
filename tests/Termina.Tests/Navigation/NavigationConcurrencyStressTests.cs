// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Termina.Application;
using Termina.Layout;
using Termina.Reactive;
using Xunit;

namespace Termina.Tests.Navigation;

/// <summary>
/// Concurrency stress tests for navigation to catch cross-thread data races on weak memory models (ARM64).
/// These tests simulate rapid navigation from background threads while the render loop runs concurrently.
/// </summary>
[Trait("Category", "Stress")]
public sealed class NavigationConcurrencyStressTests
{
    [Fact]
    public void NavigateFromBackgroundThreads_Stress_NoRaceOrException()
    {
        var app = new TerminaApplication();
        var renderComplete = new ManualResetEventSlim(false);
        var navigationComplete = new ManualResetEventSlim(false);
        var exceptions = new ConcurrentBag<Exception>();
        const int iterations = 5000;
        const int threadCount = 4;

        var pages = Enumerable.Range(0, 10).Select(_ => new StrictBindingPage()).ToList();
        app.Navigate(pages[0]);

        // Render loop thread
        var renderTask = Task.Run(() =>
        {
            while (!navigationComplete.IsSet)
            {
                try
                {
                    app.RenderCurrentPage();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                Thread.Sleep(1);
            }
        });

        // Navigation threads
        var navTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var targetPage = pages[i % pages.Count];
                    app.Navigate(targetPage);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToList();

        Task.WhenAll(navTasks).Wait();
        navigationComplete.Set();
        renderTask.Wait();

        Assert.Empty(exceptions);
        app.Dispose();
    }
}

internal sealed class StrictBindingPage : ReactivePage<ReactiveViewModel>
{
    private bool _isBound;

    protected override void OnBound()
    {
        base.OnBound();
        _isBound = true;
    }

    public override ILayoutNode BuildLayout()
    {
        if (!_isBound)
            throw new InvalidOperationException("BuildLayout called before OnBound! Race condition detected.");
        return new TextNode("Stress Test Page");
    }
}

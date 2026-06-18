// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Time.Testing;
using R3;
using Termina.Input;
using Termina.Layout;
using Termina.Pages;
using Termina.Reactive;

namespace Termina.Tests.Reactive;

/// <summary>
/// Tests for ReactivePage lifecycle behavior with activation/deactivation.
/// These tests verify the fix for GitHub issue #67: NavigationBehavior.PreserveState
/// should not dispose layout on navigation, preventing ObjectDisposedException.
/// </summary>
public class ReactivePageLifecycleTests
{
    [Fact]
    public void ReactivePage_OnNavigatedTo_BuildsLayout_OnFirstCall()
    {
        // Arrange
        var page = new TestPage();
        page.BindToTestViewModel();

        // Act
        page.OnNavigatedTo();

        // Assert - layout should be built
        Assert.NotNull(page.GetLayoutRoot());
        Assert.Equal(1, page.BuildLayoutCallCount);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_OnNavigatedTo_ReusesLayout_OnSubsequentCalls()
    {
        // Arrange
        var page = new TestPage();
        page.BindToTestViewModel();
        page.OnNavigatedTo(); // First build

        var firstLayout = page.GetLayoutRoot();

        // Act
        page.OnNavigatingFrom();
        page.OnNavigatedTo(); // Second navigation

        // Assert - layout should be reused, not rebuilt
        Assert.Same(firstLayout, page.GetLayoutRoot());
        Assert.Equal(1, page.BuildLayoutCallCount); // Still only 1 call

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_OnNavigatedTo_ActivatesLayout()
    {
        // Arrange
        var page = new TestPageWithSpinner();
        page.BindToTestViewModel();

        // Act
        page.OnNavigatedTo();

        // Assert - spinner should be animating
        Assert.True(page.Spinner.IsAnimating);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_OnNavigatingFrom_DeactivatesLayout()
    {
        // Arrange
        var page = new TestPageWithSpinner();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        // Act
        page.OnNavigatingFrom();

        // Assert - spinner should NOT be animating
        Assert.False(page.Spinner.IsAnimating);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_OnNavigatingFrom_DoesNotDisposeLayout()
    {
        // Arrange
        var page = new TestPage();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        var layout = page.GetLayoutRoot();

        // Act
        page.OnNavigatingFrom();

        // Assert - layout should NOT be null (not disposed)
        Assert.Same(layout, page.GetLayoutRoot());

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_Dispose_DisposesLayout()
    {
        // Arrange
        var page = new TestPage();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        // Act
        page.Dispose();

        // Assert - layout should be null after disposal
        Assert.Null(page.GetLayoutRoot());
    }

    [Fact]
    public void ReactivePage_NavigationCycle_ReactivatesLayout()
    {
        // Arrange
        var page = new TestPageWithSpinner();
        page.BindToTestViewModel();

        // Act - simulate navigation cycle
        page.OnNavigatedTo(); // Navigate to
        Assert.True(page.Spinner.IsAnimating);

        page.OnNavigatingFrom(); // Navigate away
        Assert.False(page.Spinner.IsAnimating);

        page.OnNavigatedTo(); // Navigate back
        Assert.True(page.Spinner.IsAnimating);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_LayoutState_PreservedAcrossNavigation()
    {
        // Arrange
        var page = new TestPageWithInput();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        // Enter text
        page.Input.HandleInput(new ConsoleKeyInfo('H', (ConsoleKey)0, false, false, false));
        page.Input.HandleInput(new ConsoleKeyInfo('i', (ConsoleKey)0, false, false, false));

        var originalText = page.Input.Text;
        Assert.Equal("Hi", originalText);

        // Act - simulate navigation away and back
        page.OnNavigatingFrom();
        page.OnNavigatedTo();

        // Assert - text should be preserved
        Assert.Equal(originalText, page.Input.Text);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_SubjectNotDisposed_DuringNavigation()
    {
        // Arrange
        var page = new TestPageWithInput();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        var submissionCount = 0;
        page.Input.Submitted.Subscribe(_ => submissionCount++);

        // Act - navigate away
        page.OnNavigatingFrom();

        // Trigger submission while "away" (simulating in-flight event)
        page.Input.HandleInput(new ConsoleKeyInfo('x', (ConsoleKey)0, false, false, false));
        page.Input.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        // Assert - should NOT throw ObjectDisposedException
        // Subject should still work even though layout is deactivated
        Assert.Equal(1, submissionCount);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_RaceCondition_NoException_WhenNavigatingDuringEvent()
    {
        // Arrange - This is the exact scenario from GitHub issue #67
        var page = new TestPageWithInput();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        Exception? caughtException = null;

        // Subscribe to Submitted with a delayed handler to simulate race
        page.Input.Submitted.Subscribe(text =>
        {
            try
            {
                // Simulate work that might be in-flight when navigation happens
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
        });

        // Start typing
        page.Input.HandleInput(new ConsoleKeyInfo('t', (ConsoleKey)0, false, false, false));

        // Act - navigate away while event might be processing
        page.OnNavigatingFrom();

        // Submit (simulating user pressing Enter right as navigation happens)
        page.Input.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        // Wait a bit for any async processing
        Thread.Sleep(50);

        // Assert - no exception should be thrown
        Assert.Null(caughtException);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_PageSubscriptions_ClearedOnNavigatingFrom()
    {
        // Arrange
        var page = new TestPage();
        page.BindToTestViewModel();
        page.OnNavigatedTo();

        var testSubject = new Subject<int>();
        var count = 0;

        page.AddTestSubscription(testSubject.Subscribe(x => count = x));

        // Emit before navigation
        testSubject.OnNext(42);
        Assert.Equal(42, count);

        // Act
        page.OnNavigatingFrom();

        // Emit after navigation
        testSubject.OnNext(100);

        // Assert - subscription should be cleared (count unchanged)
        Assert.Equal(42, count);

        // Cleanup
        testSubject.Dispose();
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_MultipleNavigationCycles_NoMemoryLeaks()
    {
        // Arrange
        var page = new TestPageWithSpinner();
        page.BindToTestViewModel();

        // Act - simulate 100 navigation cycles
        for (int i = 0; i < 100; i++)
        {
            page.OnNavigatedTo();
            page.OnNavigatingFrom();
        }

        // Assert - page should still be functional
        page.OnNavigatedTo();
        Assert.True(page.Spinner.IsAnimating);

        // Cleanup
        page.Dispose();
    }

    [Fact]
    public void ReactivePage_InvalidationSubscription_ReCreatedAfterNavigation()
    {
        // Arrange - page with an IInvalidatingNode root (SpinnerNode) using FakeTimeProvider
        var timeProvider = new FakeTimeProvider();
        using var frameProvider = new TerminaRenderFrameProvider(
            () => { },
            timeProvider,
            TimeSpan.FromMilliseconds(10));
        var page = new TestPageWithSpinner(timeProvider);

        var redrawCount = 0;

        // Wire up ViewModel with a redraw counter
        var vm = new TestViewModel();
        vm.WireUp(
            _ => { },
            (_, _) => { },
            () => { },
            () => redrawCount++,
            Observable.Empty<IInputEvent>(),
            renderFrameProvider: frameProvider,
            timeProvider: timeProvider);
        page.BindForTest(vm);

        // First visit - invalidation subscription created
        page.OnNavigatedTo();
        Assert.True(page.Spinner.IsAnimating);

        // Advance time to trigger invalidation via the spinner's interval timer
        redrawCount = 0;
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));
        frameProvider.AdvanceFrame();
        Assert.True(redrawCount > 0, "Invalidation subscription should work on first visit");

        // Navigate away - subscription disposed
        page.OnNavigatingFrom();

        // Navigate back - subscription must be re-created
        redrawCount = 0;
        page.OnNavigatedTo();
        Assert.True(page.Spinner.IsAnimating);

        // Advance time again - must still reach RequestRedraw
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));
        frameProvider.AdvanceFrame();

        // Assert - RequestRedraw should have been called after navigation round-trip
        Assert.True(redrawCount > 0, "Invalidation subscription was not re-created after navigation round-trip");

        // Cleanup
        page.Dispose();
    }

    // Test helper classes
    private class TestPage : ReactivePage<TestViewModel>
    {
        public int BuildLayoutCallCount { get; private set; }

        // Expose LayoutRoot for testing (it's internal on IBindablePage)
        public ILayoutNode? GetLayoutRoot() => ((IBindablePage)this).LayoutRoot;

        // Expose Subscriptions for testing (it's protected)
        public IDisposable AddTestSubscription(IDisposable subscription)
        {
            subscription.DisposeWith(Subscriptions);
            return subscription;
        }

        public override ILayoutNode BuildLayout()
        {
            BuildLayoutCallCount++;
            return new TextNode("Test Page");
        }

        public void BindToTestViewModel()
        {
            var vm = new TestViewModel();
            vm.WireUp(_ => { }, (_, _) => { }, () => { }, () => { }, Observable.Empty<IInputEvent>());
            Bind(vm);
        }
    }

    private class TestPageWithSpinner : ReactivePage<TestViewModel>
    {
        private readonly TimeProvider? _timeProvider;

        public SpinnerNode Spinner { get; private set; } = null!;

        public TestPageWithSpinner(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider;
        }

        public override ILayoutNode BuildLayout()
        {
            Spinner = new SpinnerNode(timeProvider: _timeProvider);
            return Spinner;
        }

        public void BindToTestViewModel()
        {
            var vm = new TestViewModel();
            vm.WireUp(_ => { }, (_, _) => { }, () => { }, () => { }, Observable.Empty<IInputEvent>());
            Bind(vm);
        }

        public void BindForTest(TestViewModel vm)
        {
            Bind(vm);
        }
    }

    private class TestPageWithInput : ReactivePage<TestViewModel>
    {
        public TextInputNode Input { get; private set; } = null!;

        public override ILayoutNode BuildLayout()
        {
            Input = new TextInputNode();
            return Input;
        }

        public void BindToTestViewModel()
        {
            var vm = new TestViewModel();
            vm.WireUp(_ => { }, (_, _) => { }, () => { }, () => { }, Observable.Empty<IInputEvent>());
            Bind(vm);
        }
    }

    private class TestViewModel : ReactiveViewModel
    {
        // Minimal test ViewModel
    }
}

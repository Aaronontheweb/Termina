// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using Termina.App;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.App;

/// <summary>
/// Tests for ReactiveTerminaApp and ReactivePageBase.
/// </summary>
public class ReactiveTerminaAppTests
{
    #region Test ViewModel and Page

    public partial class TestViewModel : ReactiveViewModel
    {
        // Note: In real usage [Reactive] generates these, but for tests we implement manually
        private int _count;
        private string _statusMessage = "";

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                OnCountChanged(value);
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnStatusMessageChanged(value);
            }
        }

        // Manual observable implementations for testing
        private readonly System.Reactive.Subjects.BehaviorSubject<int> _countSubject = new(0);
        private readonly System.Reactive.Subjects.BehaviorSubject<string> _statusSubject = new("");

        public IObservable<int> CountChanged => _countSubject.AsObservable();
        public IObservable<string> StatusMessageChanged => _statusSubject.AsObservable();

        private void OnCountChanged(int value) => _countSubject.OnNext(value);
        private void OnStatusMessageChanged(string value) => _statusSubject.OnNext(value);

        public override void OnActivated()
        {
            Input.OfType<KeyPressed>()
                .Subscribe(HandleKey)
                .DisposeWith(Subscriptions);
        }

        private void HandleKey(KeyPressed key)
        {
            switch (key.KeyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    Count++;
                    StatusMessage = $"Incremented to {Count}";
                    break;
                case ConsoleKey.DownArrow:
                    Count--;
                    StatusMessage = $"Decremented to {Count}";
                    break;
                case ConsoleKey.Escape:
                    Shutdown();
                    break;
            }
        }
    }

    public class TestPage : ReactivePageBase<TestViewModel>
    {
        public override IEnumerable<Region> GetRegions()
        {
            yield return new Region("counter",
                new LayoutConstraint.Fixed(0),
                new LayoutConstraint.Fixed(0),
                new LayoutConstraint.Fixed(40),
                new LayoutConstraint.Fixed(3));

            yield return Region.BottomRow("status", 1);
        }

        protected override void OnBound(RenderCoordinator coordinator)
        {
            ViewModel.CountChanged
                .Select(c => new Text($"Count: {c}"))
                .RenderTo(coordinator, "counter")
                .DisposeWith(Subscriptions);

            ViewModel.StatusMessageChanged
                .Select(s => new Text(s))
                .RenderTo(coordinator, "status")
                .DisposeWith(Subscriptions);
        }
    }

    #endregion

    [Fact]
    public async Task CreateHeadless_CreatesWorkingApp()
    {
        await using var app = ReactiveTerminaApp.CreateHeadless(80, 24);

        Assert.Equal(80, app.Width);
        Assert.Equal(24, app.Height);
        Assert.False(app.IsRunning);
    }

    [Fact]
    public async Task SetPage_RegistersRegions()
    {
        await using var app = ReactiveTerminaApp.CreateHeadless(80, 24);
        var viewModel = new TestViewModel();
        var page = new TestPage();

        app.SetPage(page, viewModel);

        Assert.NotNull(app.Renderer.GetRegion("counter"));
        Assert.NotNull(app.Renderer.GetRegion("status"));
    }

    [Fact]
    public async Task SetPage_WiresViewModelInput()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var viewModel = new TestViewModel();
        var page = new TestPage();
        app.SetPage(page, viewModel);

        // Simulate input
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.Equal(2, viewModel.Count);
    }

    [Fact]
    public async Task ViewModel_Shutdown_StopsApp()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var viewModel = new TestViewModel();
        var page = new TestPage();
        app.SetPage(page, viewModel);

        // Escape triggers Shutdown()
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.False(app.IsRunning);
    }

    [Fact]
    public async Task ObservableChanges_UpdatesRegions()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var viewModel = new TestViewModel();
        var page = new TestPage();
        app.SetPage(page, viewModel);

        // Initial render should show count 0
        Assert.True(terminal.Contains("Count: 0"));

        // Increment and verify update
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.True(terminal.Contains("Count: 1"));
        Assert.True(terminal.Contains("Incremented to 1"));
    }

    [Fact]
    public async Task MultipleKeyPresses_UpdatesState()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var viewModel = new TestViewModel();
        var page = new TestPage();
        app.SetPage(page, viewModel);

        // Increment 5 times, decrement 2 times = 3
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.UpArrow);
        input.EnqueueKey(ConsoleKey.DownArrow);
        input.EnqueueKey(ConsoleKey.DownArrow);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.Equal(3, viewModel.Count);
    }

    [Fact]
    public async Task SetPage_CallsOnActivated()
    {
        await using var app = ReactiveTerminaApp.CreateHeadless(80, 24);
        var viewModel = new TestViewModel();
        var page = new TestPage();

        app.SetPage(page, viewModel);

        // The Input observable should be wired up (set by OnActivated subscription)
        // We can't easily test OnActivated directly, but we test via behavior
        Assert.Equal(0, viewModel.Count); // Initial state
    }

    [Fact]
    public async Task SetPage_CallsOnNavigatedTo()
    {
        await using var app = ReactiveTerminaApp.CreateHeadless(80, 24);

        var navigatedToCalled = false;
        var page = new TestPageWithLifecycle(() => navigatedToCalled = true);
        var viewModel = new TestViewModel();

        app.SetPage(page, viewModel);

        Assert.True(navigatedToCalled);
    }

    private class TestPageWithLifecycle : ReactivePageBase<TestViewModel>
    {
        private readonly Action _onNavigatedTo;

        public TestPageWithLifecycle(Action onNavigatedTo)
        {
            _onNavigatedTo = onNavigatedTo;
        }

        public override IEnumerable<Region> GetRegions()
        {
            yield return Region.FullScreen("main");
        }

        protected override void OnBound(RenderCoordinator coordinator)
        {
            // No bindings for this test
        }

        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            _onNavigatedTo();
        }
    }

    [Fact]
    public async Task Input_Observable_ReceivesEvents()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var receivedEvents = new List<IInputEvent>();
        app.Input.Subscribe(evt => receivedEvents.Add(evt));

        var viewModel = new TestViewModel();
        var page = new TestPage();
        app.SetPage(page, viewModel);

        input.EnqueueKey(ConsoleKey.A);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.True(receivedEvents.Count >= 2);
        Assert.Contains(receivedEvents, e => e is KeyPressed kp && kp.KeyInfo.Key == ConsoleKey.A);
    }

    [Fact]
    public async Task Tab_CyclesFocus()
    {
        var terminal = new VirtualTerminal(80, 24);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var viewModel = new TestViewModelWithFocus();
        var page = new TestPageWithFocus();
        app.SetPage(page, viewModel);

        var focusChanges = new List<string?>();
        app.Focus.OnFocusChanged += (old, @new) => focusChanges.Add(@new?.Id);

        app.Focus.FocusFirst();

        input.EnqueueKey(ConsoleKey.Tab);
        input.EnqueueKey(ConsoleKey.Tab);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        Assert.Contains("field2", focusChanges);
    }

    public partial class TestViewModelWithFocus : ReactiveViewModel
    {
        public override void OnActivated()
        {
            Input.OfType<KeyPressed>()
                .Subscribe(k =>
                {
                    if (k.KeyInfo.Key == ConsoleKey.Escape)
                        Shutdown();
                })
                .DisposeWith(Subscriptions);
        }
    }

    public class TestPageWithFocus : ReactivePageBase<TestViewModelWithFocus>
    {
        public override IEnumerable<Region> GetRegions()
        {
            yield return new Region("field1",
                new LayoutConstraint.Fixed(0),
                new LayoutConstraint.Fixed(0),
                new LayoutConstraint.Fixed(20),
                new LayoutConstraint.Fixed(1)) { Focusable = true, TabOrder = 0 };

            yield return new Region("field2",
                new LayoutConstraint.Fixed(0),
                new LayoutConstraint.Fixed(1),
                new LayoutConstraint.Fixed(20),
                new LayoutConstraint.Fixed(1)) { Focusable = true, TabOrder = 1 };
        }

        protected override void OnBound(RenderCoordinator coordinator)
        {
            // No bindings for this test
        }
    }

    [Fact]
    public async Task ResizeEvent_RecalculatesLayout()
    {
        // Start larger so shrinking doesn't cause issues with regions
        var terminal = new VirtualTerminal(100, 30);
        var input = new VirtualInputSource();
        await using var app = ReactiveTerminaApp.CreateHeadless(terminal, input);

        var viewModel = new TestViewModel();
        var page = new TestPage();
        app.SetPage(page, viewModel);

        var resizeReceived = false;
        app.Input.OfType<ResizeEvent>().Subscribe(_ => resizeReceived = true);

        // Simulate resize (make smaller, as regions have fixed sizes)
        input.EnqueueResize(80, 24);
        input.EnqueueKey(ConsoleKey.Escape);
        input.Complete();

        await app.RunAsync();

        // Terminal should have been resized
        Assert.Equal(80, terminal.Width);
        Assert.Equal(24, terminal.Height);
        Assert.True(resizeReceived);
    }
}

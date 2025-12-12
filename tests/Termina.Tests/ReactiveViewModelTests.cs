using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Tests;

/// <summary>
/// Tests for the reactive ViewModel infrastructure.
/// </summary>
public class ReactiveViewModelTests
{
    [Fact]
    public void ReactiveViewModel_CanBeCreated()
    {
        var vm = new TestViewModel();
        Assert.NotNull(vm);
    }

    [Fact]
    public void ReactiveViewModel_SubscriptionsDisposedOnDispose()
    {
        var vm = new TestViewModel();
        var disposed = false;

        vm.TestSubscriptions.Add(Disposable.Create(() => disposed = true));

        vm.Dispose();

        Assert.True(disposed);
    }

    [Fact]
    public void ReactiveViewModel_NavigateActionWorks()
    {
        var vm = new TestViewModel();
        string? navigatedTo = null;

        vm.WireUp(
            navigate: key => navigatedTo = key,
            shutdown: () => { },
            input: Observable.Empty<IInputEvent>());

        vm.TestNavigate("test-page");

        Assert.Equal("test-page", navigatedTo);
    }

    [Fact]
    public void ReactiveViewModel_ShutdownActionWorks()
    {
        var vm = new TestViewModel();
        var shutdownCalled = false;

        vm.WireUp(
            navigate: _ => { },
            shutdown: () => shutdownCalled = true,
            input: Observable.Empty<IInputEvent>());

        vm.TestShutdown();

        Assert.True(shutdownCalled);
    }

    [Fact]
    public void ReactiveViewModel_InputObservableWorks()
    {
        var vm = new TestViewModel();
        var inputSubject = new Subject<IInputEvent>();
        var receivedKeys = new List<ConsoleKey>();

        vm.WireUp(
            navigate: _ => { },
            shutdown: () => { },
            input: inputSubject);

        vm.TestInput.OfType<KeyPressed>()
            .Subscribe(k => receivedKeys.Add(k.KeyInfo.Key))
            .DisposeWith(vm.TestSubscriptions);

        inputSubject.OnNext(new KeyPressed(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false)));
        inputSubject.OnNext(new KeyPressed(new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false)));

        Assert.Equal(2, receivedKeys.Count);
        Assert.Equal(ConsoleKey.A, receivedKeys[0]);
        Assert.Equal(ConsoleKey.B, receivedKeys[1]);
    }

    [Fact]
    public void ReactiveViewModel_OnActivatedCalled()
    {
        var vm = new TestViewModel();
        vm.WireUp(_ => { }, () => { }, Observable.Empty<IInputEvent>());

        Assert.False(vm.WasActivated);

        vm.OnActivated();

        Assert.True(vm.WasActivated);
    }

    [Fact]
    public void ReactiveViewModel_OnDeactivatingCalled()
    {
        var vm = new TestViewModel();
        vm.WireUp(_ => { }, () => { }, Observable.Empty<IInputEvent>());

        Assert.False(vm.WasDeactivating);

        vm.OnDeactivating();

        Assert.True(vm.WasDeactivating);
    }

    private class TestViewModel : ReactiveViewModel
    {
        public bool WasActivated { get; private set; }
        public bool WasDeactivating { get; private set; }

        // Expose protected members for testing
        public CompositeDisposable TestSubscriptions => Subscriptions;
        public IObservable<IInputEvent> TestInput => Input;

        public void TestNavigate(string pageKey) => Navigate(pageKey);
        public void TestShutdown() => Shutdown();

        public override void OnActivated()
        {
            base.OnActivated();
            WasActivated = true;
        }

        public override void OnDeactivating()
        {
            base.OnDeactivating();
            WasDeactivating = true;
        }
    }
}

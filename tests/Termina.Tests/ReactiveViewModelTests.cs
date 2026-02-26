using R3;
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
            navigateWithParams: (_, _) => { },
            shutdown: () => { },
            requestRedraw: () => { },
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
            navigateWithParams: (_, _) => { },
            shutdown: () => shutdownCalled = true,
            requestRedraw: () => { },
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
            navigateWithParams: (_, _) => { },
            shutdown: () => { },
            requestRedraw: () => { },
            input: inputSubject);

        vm.TestInput.OfType<IInputEvent, KeyPressed>()
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
        vm.WireUp(_ => { }, (_, _) => { }, () => { }, () => { }, Observable.Empty<IInputEvent>());

        Assert.False(vm.WasActivated);

        vm.OnActivated();

        Assert.True(vm.WasActivated);
    }

    [Fact]
    public void ReactiveViewModel_OnDeactivatingCalled()
    {
        var vm = new TestViewModel();
        vm.WireUp(_ => { }, (_, _) => { }, () => { }, () => { }, Observable.Empty<IInputEvent>());

        Assert.False(vm.WasDeactivating);

        vm.OnDeactivating();

        Assert.True(vm.WasDeactivating);
    }

    [Fact]
    public void ReactiveViewModel_WithReactiveProperties_GetSetWorks()
    {
        var vm = new TestReactiveViewModel();

        Assert.NotNull(vm);
        vm.Count.Value = 5;
        Assert.Equal(5, vm.Count.Value);

        // Dispose should dispose reactive properties
        vm.Dispose();

        // After disposal, setting a value should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => vm.Count.Value = 10);
    }

    [Fact]
    public void ReactiveViewModel_WithReactiveProperties_DisposesAll()
    {
        var vm = new TestReactiveViewModel();

        // Set some values to ensure properties are active
        vm.Count.Value = 42;
        vm.Message.Value = "Hello";

        Assert.Equal(42, vm.Count.Value);
        Assert.Equal("Hello", vm.Message.Value);

        // Dispose the ViewModel
        vm.Dispose();

        // Both properties should be disposed
        Assert.Throws<ObjectDisposedException>(() => vm.Count.Value = 1);
        Assert.Throws<ObjectDisposedException>(() => vm.Message.Value = "test");
    }

    [Fact]
    public void ReactiveViewModel_WithReactiveProperties_CannotSetValueAfterDispose()
    {
        var vm = new TestReactiveViewModel();
        var valuesReceived = new List<int>();

        // Subscribe to track values
        vm.Count.Subscribe(v => valuesReceived.Add(v));

        // Set a value before dispose
        vm.Count.Value = 42;
        Assert.Contains(42, valuesReceived);

        // Dispose
        vm.Dispose();

        // Attempting to set value after dispose throws
        Assert.Throws<ObjectDisposedException>(() => vm.Count.Value = 100);
    }

    [Fact]
    public void ReactiveViewModel_WithCustomDispose_CallsCustomDispose()
    {
        var vm = new TestReactiveViewModelWithCustomDispose();

        // Set a value
        vm.Value.Value = 42;
        Assert.Equal(42, vm.Value.Value);

        // Dispose
        vm.Dispose();

        // Custom dispose was called
        Assert.True(vm.CustomDisposeWasCalled);

        // Reactive property was disposed (setting value throws)
        Assert.Throws<ObjectDisposedException>(() => vm.Value.Value = 100);
    }

    [Fact]
    public void ReactiveViewModel_WithNullableProperties_PreservesNullability()
    {
        var vm = new TestReactiveViewModelWithNullable();

        // Properties should be nullable and initially null
        Assert.Null(vm.NullableString.Value);
        Assert.Null(vm.NullableInt.Value);
        Assert.Null(vm.NullableObject.Value);

        // Can set to non-null values
        vm.NullableString.Value = "test";
        vm.NullableInt.Value = 42;
        vm.NullableObject.Value = new object();

        Assert.Equal("test", vm.NullableString.Value);
        Assert.Equal(42, vm.NullableInt.Value);
        Assert.NotNull(vm.NullableObject.Value);

        // Can set back to null
        vm.NullableString.Value = null;
        vm.NullableInt.Value = null;
        vm.NullableObject.Value = null;

        Assert.Null(vm.NullableString.Value);
        Assert.Null(vm.NullableInt.Value);
        Assert.Null(vm.NullableObject.Value);

        vm.Dispose();
    }

    private class TestViewModel : ReactiveViewModel
    {
        public bool WasActivated { get; private set; }
        public bool WasDeactivating { get; private set; }

        // Expose protected members for testing
        public CompositeDisposable TestSubscriptions => Subscriptions;
        public Observable<IInputEvent> TestInput => Input;

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

/// <summary>
/// Test ViewModel with ReactiveProperty fields to verify disposal.
/// </summary>
public class TestReactiveViewModel : ReactiveViewModel
{
    public ReactiveProperty<int> Count { get; } = new(0);
    public ReactiveProperty<string> Message { get; } = new("Initial");

    public override void Dispose()
    {
        Count.Dispose();
        Message.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Test ViewModel with custom Dispose() that properly disposes reactive properties.
/// </summary>
public class TestReactiveViewModelWithCustomDispose : ReactiveViewModel
{
    public ReactiveProperty<int> Value { get; } = new(0);

    public bool CustomDisposeWasCalled { get; private set; }

    public override void Dispose()
    {
        CustomDisposeWasCalled = true;
        Value.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Test ViewModel with nullable ReactiveProperty fields to verify nullability.
/// </summary>
public class TestReactiveViewModelWithNullable : ReactiveViewModel
{
    public ReactiveProperty<string?> NullableString { get; } = new(null);
    public ReactiveProperty<int?> NullableInt { get; } = new(null);
    public ReactiveProperty<object?> NullableObject { get; } = new(null);

    public override void Dispose()
    {
        NullableString.Dispose();
        NullableInt.Dispose();
        NullableObject.Dispose();
        base.Dispose();
    }
}

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
    public void ReactiveViewModel_WithReactiveFields_HasDisposeReactiveFieldsMethod()
    {
        var vm = new TestReactiveViewModel();

        // The generated DisposeReactiveFields method should be callable
        // It's generated as protected, so we access it through Dispose()
        Assert.NotNull(vm);
        vm.Count = 5;
        Assert.Equal(5, vm.Count);

        // Dispose should call DisposeReactiveFields automatically
        vm.Dispose();

        // After disposal, setting a value should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => vm.Count = 10);
    }

    [Fact]
    public void ReactiveViewModel_WithReactiveFields_DisposesAllSubjects()
    {
        var vm = new TestReactiveViewModel();

        // Set some values to ensure subjects are active
        vm.Count = 42;
        vm.Message = "Hello";

        Assert.Equal(42, vm.Count);
        Assert.Equal("Hello", vm.Message);

        // Dispose the ViewModel
        vm.Dispose();

        // Both subjects should be disposed
        Assert.Throws<ObjectDisposedException>(() => vm.Count = 1);
        Assert.Throws<ObjectDisposedException>(() => vm.Message = "test");
    }

    [Fact]
    public void ReactiveViewModel_WithReactiveFields_CannotSetValueAfterDispose()
    {
        var vm = new TestReactiveViewModel();
        var valuesReceived = new List<int>();

        // Subscribe to track values
        vm.CountChanged.Subscribe(v => valuesReceived.Add(v));

        // Set a value before dispose
        vm.Count = 42;
        Assert.Contains(42, valuesReceived);

        // Dispose
        vm.Dispose();

        // Attempting to set value after dispose throws
        Assert.Throws<ObjectDisposedException>(() => vm.Count = 100);
    }

    [Fact]
    public void ReactiveViewModel_WithCustomDispose_CallsDisposeReactiveFields()
    {
        var vm = new TestReactiveViewModelWithCustomDispose();

        // Set a value
        vm.Value = 42;
        Assert.Equal(42, vm.Value);

        // Dispose
        vm.Dispose();

        // Custom dispose was called
        Assert.True(vm.CustomDisposeWasCalled);

        // Reactive fields were disposed (setting value throws)
        Assert.Throws<ObjectDisposedException>(() => vm.Value = 100);
    }

    [Fact]
    public void ReactiveViewModel_WithNullableFields_PreservesNullability()
    {
        var vm = new TestReactiveViewModelWithNullable();

        // Properties should be nullable and initially null
        Assert.Null(vm.NullableString);
        Assert.Null(vm.NullableInt);
        Assert.Null(vm.NullableObject);

        // Can set to non-null values
        vm.NullableString = "test";
        vm.NullableInt = 42;
        vm.NullableObject = new object();

        Assert.Equal("test", vm.NullableString);
        Assert.Equal(42, vm.NullableInt);
        Assert.NotNull(vm.NullableObject);

        // Can set back to null
        vm.NullableString = null;
        vm.NullableInt = null;
        vm.NullableObject = null;

        Assert.Null(vm.NullableString);
        Assert.Null(vm.NullableInt);
        Assert.Null(vm.NullableObject);

        vm.Dispose();
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

/// <summary>
/// Test ViewModel with [Reactive] fields to verify source generator disposal.
/// This must be partial and outside the test class for the generator to work.
/// </summary>
public partial class TestReactiveViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _message = "Initial";
}

/// <summary>
/// Test ViewModel with custom Dispose() that properly calls DisposeReactiveFields().
/// This should NOT trigger TERMINA001 error.
/// </summary>
public partial class TestReactiveViewModelWithCustomDispose : ReactiveViewModel
{
    [Reactive] private int _value;

    public bool CustomDisposeWasCalled { get; private set; }

    public override void Dispose()
    {
        CustomDisposeWasCalled = true;
        DisposeReactiveFields();
        base.Dispose();
    }
}

/// <summary>
/// Test ViewModel with nullable [Reactive] fields to verify nullability annotations are preserved.
/// </summary>
public partial class TestReactiveViewModelWithNullable : ReactiveViewModel
{
    [Reactive] private string? _nullableString = null;
    [Reactive] private int? _nullableInt = null;
    [Reactive] private object? _nullableObject = null;
}


// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;

namespace Termina.Tests.Reactive;

/// <summary>
/// Tests for <see cref="ReactivePage{TViewModel}.InvalidateLayout"/>. The
/// invalidation hook lets consumers force a <see cref="ReactivePage{T}.BuildLayout"/>
/// rebuild when external state captured at build time has changed (e.g.,
/// terminal dimensions baked into a <c>HeightAuto</c> constraint that
/// needs to track resize).
/// </summary>
public class ReactivePageInvalidateLayoutTests
{
    [Fact]
    public void InvalidateLayout_TriggersBuildLayoutAgain()
    {
        var page = new CountingPage();
        page.BindForTest(new EmptyViewModel());

        page.OnNavigatedTo();
        Assert.Equal(1, page.BuildLayoutCallCount);

        page.CallInvalidateLayout();

        Assert.Equal(2, page.BuildLayoutCallCount);
    }

    [Fact]
    public void InvalidateLayout_ReplacesLayoutRoot()
    {
        var page = new CountingPage();
        page.BindForTest(new EmptyViewModel());
        page.OnNavigatedTo();

        var firstRoot = ((Pages.IBindablePage)page).LayoutRoot;
        Assert.NotNull(firstRoot);

        page.CallInvalidateLayout();

        var secondRoot = ((Pages.IBindablePage)page).LayoutRoot;
        Assert.NotNull(secondRoot);
        Assert.NotSame(firstRoot, secondRoot);
    }

    [Fact]
    public void InvalidateLayout_PreservesUserSubscriptions()
    {
        var page = new CountingPage();
        page.BindForTest(new EmptyViewModel());
        page.OnNavigatedTo();

        // Add a user subscription tracking some external signal.
        var subject = new Subject<int>();
        var received = new List<int>();
        page.AddUserSubscription(subject.Subscribe(received.Add));

        subject.OnNext(1);
        page.CallInvalidateLayout();
        subject.OnNext(2);

        // Subscriptions added to the protected Subscriptions disposable are
        // only cleared by the framework on navigation away or disposal; an
        // InvalidateLayout must not disturb them.
        Assert.Equal(new[] { 1, 2 }, received);
    }

    [Fact]
    public void InvalidateLayout_PreservesKeyBindings()
    {
        var page = new CountingPage();
        page.BindForTest(new EmptyViewModel());
        page.OnNavigatedTo();

        page.KeyBindings.Register(ConsoleKey.Escape, () => { });
        page.KeyBindings.Register(ConsoleKey.Tab, () => { });
        Assert.Equal(2, page.KeyBindings.Count);

        page.CallInvalidateLayout();

        Assert.Equal(2, page.KeyBindings.Count);
    }

    [Fact]
    public void InvalidateLayout_SeesUpdatedExternalStateInBuildLayout()
    {
        // Concrete consumer scenario: BuildLayout's output reflects a value
        // that changes between calls (e.g., terminal width). After
        // InvalidateLayout, BuildLayout's next invocation must see the
        // current value, not the captured-at-first-render value.
        var page = new ExternalStatePage();
        page.BindForTest(new EmptyViewModel());

        page.ExternalValue = 10;
        page.OnNavigatedTo();
        Assert.Equal(10, page.LastObservedValue);

        page.ExternalValue = 42;
        page.CallInvalidateLayout();

        Assert.Equal(42, page.LastObservedValue);
    }

    private class CountingPage : ReactivePage<EmptyViewModel>
    {
        public int BuildLayoutCallCount { get; private set; }

        public override ILayoutNode BuildLayout()
        {
            BuildLayoutCallCount++;
            return new TextNode("test");
        }

        public void BindForTest(EmptyViewModel vm) => Bind(vm);
        public void CallInvalidateLayout() => InvalidateLayout();
        public void AddUserSubscription(IDisposable d) => Subscriptions.Add(d);
        public new PageKeyBindings KeyBindings => base.KeyBindings;
    }

    private sealed class ExternalStatePage : ReactivePage<EmptyViewModel>
    {
        public int ExternalValue { get; set; }
        public int LastObservedValue { get; private set; }

        public override ILayoutNode BuildLayout()
        {
            LastObservedValue = ExternalValue;
            return new TextNode(ExternalValue.ToString());
        }

        public void BindForTest(EmptyViewModel vm) => Bind(vm);
        public void CallInvalidateLayout() => InvalidateLayout();
    }

    private class EmptyViewModel : ReactiveViewModel
    {
    }
}

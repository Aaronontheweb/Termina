// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Extensions;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the DynamicLayoutNode class.
/// </summary>
public class DynamicLayoutNodeTests
{
    [Fact]
    public void DynamicLayoutNode_Measure_EvaluatesFactory()
    {
        var child = new TextNode("Hello");
        var node = new DynamicLayoutNode(() => child);

        var size = node.Measure(new Size(80, 24));

        Assert.True(size.Width > 0);
    }

    [Fact]
    public void DynamicLayoutNode_Render_EvaluatesFactory()
    {
        var renderCalled = false;
        var child = new SpyRenderNode(() => renderCalled = true);
        var node = new DynamicLayoutNode(() => child);

        var context = new NullRenderContext();
        node.Render(context, new Rect(0, 0, 80, 24));

        Assert.True(renderCalled);
    }

    [Fact]
    public void DynamicLayoutNode_SameInstance_NoLifecycleChurn()
    {
        var child = new SpyLifecycleNode();
        var node = new DynamicLayoutNode(() => child);
        node.OnActivate();

        // Measure twice — same instance returned, should not deactivate/reactivate
        node.Measure(new Size(80, 24));
        var activateCount = child.ActivateCount;
        var deactivateCount = child.DeactivateCount;

        node.Measure(new Size(80, 24));

        Assert.Equal(activateCount, child.ActivateCount);
        Assert.Equal(deactivateCount, child.DeactivateCount);
    }

    [Fact]
    public void DynamicLayoutNode_ChildChange_DeactivatesOldActivatesNew()
    {
        var childA = new SpyLifecycleNode();
        var childB = new SpyLifecycleNode();
        ILayoutNode current = childA;

        var node = new DynamicLayoutNode(() => current);
        node.OnActivate();

        // First evaluation activates childA
        node.Measure(new Size(80, 24));
        Assert.Equal(1, childA.ActivateCount);

        // Switch to childB
        current = childB;
        node.Measure(new Size(80, 24));

        Assert.Equal(1, childA.DeactivateCount);
        Assert.Equal(1, childB.ActivateCount);
    }

    [Fact]
    public void DynamicLayoutNode_Invalidate_FiresInvalidatedObservable()
    {
        var node = new DynamicLayoutNode(() => new EmptyNode());
        var invalidationCount = 0;

        node.Invalidated.Subscribe(_ => invalidationCount++);
        node.Invalidate();

        Assert.Equal(1, invalidationCount);
    }

    [Fact]
    public void DynamicLayoutNode_ChildInvalidation_PropagatesUpward()
    {
        var childSubject = new Subject<Unit>();
        var child = new SpyInvalidatingNode(childSubject);
        var node = new DynamicLayoutNode(() => child);
        node.OnActivate();

        var parentInvalidations = 0;
        node.Invalidated.Subscribe(_ => parentInvalidations++);

        // Evaluate factory so child gets subscribed
        node.Measure(new Size(80, 24));

        childSubject.OnNext(Unit.Default);

        Assert.Equal(1, parentInvalidations);
    }

    [Fact]
    public void DynamicLayoutNode_FluentSizing_Works()
    {
        var node = new DynamicLayoutNode(() => new EmptyNode());

        var result = node.Width(40).Height(10);

        Assert.IsType<SizeConstraint.Fixed>(result.WidthConstraint);
        Assert.IsType<SizeConstraint.Fixed>(result.HeightConstraint);
    }

    [Fact]
    public void DynamicLayoutNode_OnActivate_ActivatesCurrentChild()
    {
        var child = new SpyLifecycleNode();
        var node = new DynamicLayoutNode(() => child);

        // Evaluate so child is set
        node.Measure(new Size(80, 24));
        Assert.Equal(0, child.ActivateCount); // Not active yet

        node.OnActivate();

        Assert.Equal(1, child.ActivateCount);
    }

    [Fact]
    public void DynamicLayoutNode_OnDeactivate_DeactivatesCurrentChild()
    {
        var child = new SpyLifecycleNode();
        var node = new DynamicLayoutNode(() => child);

        node.Measure(new Size(80, 24));
        node.OnActivate();
        node.OnDeactivate();

        Assert.Equal(1, child.DeactivateCount);
    }

    [Fact]
    public void DynamicLayoutNode_Dispose_DisposesCurrentChild()
    {
        var child = new SpyLifecycleNode();
        var node = new DynamicLayoutNode(() => child);

        node.Measure(new Size(80, 24));
        node.Dispose();

        Assert.True(child.WasDisposed);
    }

    [Fact]
    public void DynamicLayoutNode_GetChildNodes_ReturnsCurrentChild()
    {
        var child = new TextNode("test");
        var node = new DynamicLayoutNode(() => child);

        // Evaluate factory
        node.Measure(new Size(80, 24));

        var children = node.GetChildNodes().ToList();
        Assert.Single(children);
        Assert.Same(child, children[0]);
    }

    [Fact]
    public void DynamicLayoutNode_BeforeActivation_DoesNotActivateChildOnChange()
    {
        var childA = new SpyLifecycleNode();
        var childB = new SpyLifecycleNode();
        ILayoutNode current = childA;

        var node = new DynamicLayoutNode(() => current);

        // Evaluate to set childA
        node.Measure(new Size(80, 24));

        // Switch to childB while NOT active
        current = childB;
        node.Measure(new Size(80, 24));

        Assert.Equal(0, childB.ActivateCount);
    }

    [Fact]
    public void DynamicLayoutNode_AsDynamicLayout_Extension_Works()
    {
        var trigger = new Subject<Unit>();
        var child = new TextNode("test");
        Func<ILayoutNode> factory = () => child;

        var node = factory.AsDynamicLayout(trigger);
        var invalidationCount = 0;
        node.Invalidated.Subscribe(_ => invalidationCount++);

        trigger.OnNext(Unit.Default);

        Assert.Equal(1, invalidationCount);
    }

    #region Test helpers

    private class SpyLifecycleNode : LayoutNode
    {
        public int ActivateCount { get; private set; }
        public int DeactivateCount { get; private set; }
        public bool WasDisposed { get; private set; }

        public override void OnActivate()
        {
            ActivateCount++;
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            DeactivateCount++;
            base.OnDeactivate();
        }

        public override void Dispose()
        {
            WasDisposed = true;
            base.Dispose();
        }

        public override Size Measure(Size available) => new(10, 1);
        public override void Render(IRenderContext context, Rect bounds) { }
    }

    private class SpyRenderNode : LayoutNode
    {
        private readonly Action _onRender;

        public SpyRenderNode(Action onRender) => _onRender = onRender;

        public override Size Measure(Size available) => new(10, 1);

        public override void Render(IRenderContext context, Rect bounds) => _onRender();
    }

    private class SpyInvalidatingNode : LayoutNode, IInvalidatingNode
    {
        public Observable<Unit> Invalidated { get; }

        public SpyInvalidatingNode(Observable<Unit> invalidated)
        {
            Invalidated = invalidated;
        }

        public override Size Measure(Size available) => new(10, 1);
        public override void Render(IRenderContext context, Rect bounds) { }
    }

    private class NullRenderContext : IRenderContext
    {
        public int Width => 80;
        public int Height => 24;
        public void WriteAt(int x, int y, string text) { }
        public void WriteAt(int x, int y, char c) { }
        public void SetForeground(Color color) { }
        public void SetBackground(Color color) { }
        public void ResetColors() { }
        public void SetDecoration(TextDecoration decoration) { }
        public void ApplyStyle(TextStyle style) { }
        public void Fill(int x, int y, int width, int height, char c = ' ') { }
        public void Clear() { }
        public IRenderContext CreateSubContext(Rect bounds) => this;
    }

    #endregion
}

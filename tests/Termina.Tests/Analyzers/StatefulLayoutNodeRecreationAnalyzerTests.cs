// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Termina.Generators;
using Termina.Tests.Utility;

namespace Termina.Tests.Analyzers;

using Verify = TerminaVerifier<StatefulLayoutNodeRecreationAnalyzer>;

/// <summary>
/// Tests for <see cref="StatefulLayoutNodeRecreationAnalyzer"/> (TERMINA004).
///
/// The warn cases (W*) show a factory that creates a new stateful node on every run. The no-warn cases (N*)
/// show correct code that reuses a stateful node or invalidates a narrower node. The location cases (L*)
/// check the reported position and the field-to-factory link. Each warn case marks the expected diagnostic
/// location with `{|#0:Invalidate|}` markup, so the tests do not depend on manual line numbers.
/// </summary>
public sealed class StatefulLayoutNodeRecreationAnalyzerTests
{
    private const string TerminaFrameworkSource = """
        namespace Termina.Layout
        {
            public interface ILayoutNode { }
            public abstract class LayoutNode : ILayoutNode { }

            public sealed class DynamicLayoutNode : LayoutNode
            {
                public DynamicLayoutNode(System.Func<ILayoutNode> factory) { }
                public void Invalidate() { }
            }

            public sealed class KeyedDynamicLayoutNode<TKey> : LayoutNode
            {
                public KeyedDynamicLayoutNode(System.Func<TKey> keySelector, System.Func<TKey, ILayoutNode> contentFactory) { }
                public void Invalidate() { }
            }

            public sealed class TextNode : LayoutNode
            {
                public TextNode(string text) { }
            }

            public sealed class ScrollableContainerNode : LayoutNode
            {
                public ScrollableContainerNode WithContent(ILayoutNode content) => this;
            }

            public sealed class TextInputNode : LayoutNode { }
            public sealed class StreamingTextNode : LayoutNode { }
            public sealed class SelectionListNode<T> : LayoutNode { }

            public sealed class OtherInvalidatingNode : LayoutNode
            {
                public void Invalidate() { }
            }
        }
        """;

    private static DiagnosticResult Warning(string dynamicField, string statefulType)
        => Verify.Diagnostic(StatefulLayoutNodeRecreationAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments(dynamicField, statefulType)
            .WithSeverity(DiagnosticSeverity.Warning);

    // ---- Warn cases ------------------------------------------------------------------------------------

    [Fact] // W1: factory assigns a new stateful node to a field, broad invalidation.
    public async Task W1_ReassignScrollableField_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() =>
                    {
                        _scrollNode = new ScrollableContainerNode();
                        return _scrollNode;
                    });
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "ScrollableContainerNode"));
    }

    [Fact] // W2: factory clears the field, then creates a new instance.
    public async Task W2_ClearThenRecreateScrollableField_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() =>
                    {
                        _scrollNode = null;
                        _scrollNode = new ScrollableContainerNode();
                        return _scrollNode;
                    });
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "ScrollableContainerNode"));
    }

    [Fact] // W3: factory returns a freshly-built stateful node, no field. The main #155 footgun.
    public async Task W3_DirectReturnOfNewSelectionList_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private int _step;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() =>
                    {
                        if (_step == 0)
                            return new SelectionListNode<int>();
                        return new TextNode("done");
                    });
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "SelectionListNode"));
    }

    [Fact] // W4: the factory calls a private helper that creates the stateful node.
    public async Task W4_RecreateViaPrivateHelper_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() => Build());
                    return _contentNode;
                }

                private ILayoutNode Build()
                {
                    _scrollNode = new ScrollableContainerNode();
                    return _scrollNode;
                }

                public void OnStateChanged() => _contentNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "ScrollableContainerNode"));
    }

    [Fact] // W5a: TextInputNode recreation.
    public async Task W5a_RecreateTextInput_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() => new TextInputNode());
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "TextInputNode"));
    }

    [Fact] // W5b: StreamingTextNode recreation.
    public async Task W5b_RecreateStreamingText_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() => new StreamingTextNode());
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "StreamingTextNode"));
    }

    // ---- No-warn cases ---------------------------------------------------------------------------------

    [Fact] // N1: `??=` creates the node one time and reuses it. This was the earlier false positive.
    public async Task N1_LazyInitWithCoalesce_DoesNotReport()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() =>
                    {
                        _scrollNode ??= new ScrollableContainerNode();
                        return _scrollNode;
                    });
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.Invalidate();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    [Fact] // N2: the field is created once in BuildLayout. The factory only references it.
    public async Task N2_ReuseFieldBuiltInBuildLayout_DoesNotReport()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private DynamicLayoutNode? _rowsNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _scrollNode = new ScrollableContainerNode();
                    _rowsNode = new DynamicLayoutNode(() => new TextNode("rows"));
                    _contentNode = new DynamicLayoutNode(() => _scrollNode.WithContent(_rowsNode));
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.Invalidate();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    [Fact] // N3: the code invalidates the narrower rows node, not the parent that rebuilds the scroll node.
    public async Task N3_InvalidateNarrowerChild_DoesNotReport()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private DynamicLayoutNode? _rowsNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() =>
                    {
                        _scrollNode = new ScrollableContainerNode();
                        _rowsNode = new DynamicLayoutNode(() => new TextNode("rows"));
                        return _scrollNode.WithContent(_rowsNode);
                    });
                    return _contentNode;
                }

                public void OnRowsChanged() => _rowsNode?.Invalidate();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    [Fact] // N4: the factory returns a stateless node.
    public async Task N4_StatelessFactory_DoesNotReport()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() => new TextNode("content"));
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.Invalidate();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    [Fact] // N5: KeyedDynamicLayoutNode caches by key and preserves state. Its Invalidate must not warn.
    public async Task N5_KeyedDynamicLayoutNode_DoesNotReport()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private KeyedDynamicLayoutNode<int>? _contentNode;
                private int _step;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new KeyedDynamicLayoutNode<int>(() => _step, key => new SelectionListNode<int>());
                    return _contentNode;
                }

                public void OnStateChanged() => _contentNode?.Invalidate();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    [Fact] // N6: Invalidate() on a field that is not a DynamicLayoutNode.
    public async Task N6_InvalidateOnNonDynamicField_DoesNotReport()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private OtherInvalidatingNode? _otherNode;
                private ScrollableContainerNode? _scrollNode;

                public ILayoutNode BuildLayout()
                {
                    _otherNode = new OtherInvalidatingNode();
                    _scrollNode = new ScrollableContainerNode();
                    return _otherNode;
                }

                public void OnStateChanged() => _otherNode?.Invalidate();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    // ---- Location and robustness cases -----------------------------------------------------------------

    [Fact] // L2: a direct (non-conditional) Invalidate() call reports at the call site.
    public async Task L2_DirectInvalidateCall_ReportsAtCallSite()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode _contentNode = null!;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() => new ScrollableContainerNode());
                    return _contentNode;
                }

                public void OnStateChanged()
                {
                    _contentNode.{|#0:Invalidate|}();
                }
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "ScrollableContainerNode"));
    }

    [Fact] // L3: the Invalidate() call inside a conditional block is still detected.
    public async Task L3_InvalidateInsideIf_ReportsWarning()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _contentNode;
                private bool _dirty;

                public ILayoutNode BuildLayout()
                {
                    _contentNode = new DynamicLayoutNode(() => new ScrollableContainerNode());
                    return _contentNode;
                }

                public void OnStateChanged()
                {
                    if (_dirty)
                        _contentNode?.{|#0:Invalidate|}();
                }
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_contentNode", "ScrollableContainerNode"));
    }

    [Fact] // L4: two dynamic fields exist. Only the one with the recreating factory warns.
    public async Task L4_OnlyRecreatingDynamicFieldWarns()
    {
        const string testCode = """
            using Termina.Layout;

            public sealed class TestPage
            {
                private DynamicLayoutNode? _statelessNode;
                private DynamicLayoutNode? _statefulNode;

                public ILayoutNode BuildLayout()
                {
                    _statelessNode = new DynamicLayoutNode(() => new TextNode("safe"));
                    _statefulNode = new DynamicLayoutNode(() => new ScrollableContainerNode());
                    return _statefulNode;
                }

                public void OnSafeChanged() => _statelessNode?.Invalidate();
                public void OnStatefulChanged() => _statefulNode?.{|#0:Invalidate|}();
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], Warning("_statefulNode", "ScrollableContainerNode"));
    }
}

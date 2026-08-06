// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Termina.Generators;
using Termina.Tests.Utility;

namespace Termina.Tests.Analyzers;

using Verify = TerminaVerifier<StatefulLayoutNodeRecreationAnalyzer>;

/// <summary>
/// Tests for <see cref="StatefulLayoutNodeRecreationAnalyzer"/>.
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

            public sealed class TextNode : LayoutNode
            {
                public TextNode(string text) { }
            }

            public sealed class ScrollableContainerNode : LayoutNode { }
            public sealed class TextInputNode : LayoutNode { }
            public sealed class StreamingTextNode : LayoutNode { }
            public sealed class SelectionListNode<T> : LayoutNode { }
        }
        """;

    [Fact]
    public async Task InvalidateDynamicLayoutThatMutatesScrollableField_ReportsWarning()
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
                        return new TextNode("content");
                    });

                    return _contentNode;
                }

                public void OnStateChanged()
                {
                    _contentNode?.Invalidate();
                }
            }
            """;

        var expected = Verify.Diagnostic(StatefulLayoutNodeRecreationAnalyzer.DiagnosticId)
            .WithLocation("/0/Test1.cs", 21, 23)
            .WithArguments("_contentNode", "_scrollNode")
            .WithSeverity(DiagnosticSeverity.Warning);

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], expected);
    }

    [Fact]
    public async Task InvalidateDynamicLayoutWithStatelessFactory_DoesNotReport()
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

                public void OnStateChanged()
                {
                    _contentNode?.Invalidate();
                }
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    [Fact]
    public async Task InvalidateNarrowChildDynamicLayout_DoesNotReportForParentStatefulFactory()
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
                        _scrollNode = null;
                        _rowsNode = new DynamicLayoutNode(() => new TextNode("rows"));
                        return new TextNode("content");
                    });

                    return _contentNode;
                }

                public void OnStateChanged()
                {
                    _rowsNode?.Invalidate();
                }
            }
            """;

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }
}

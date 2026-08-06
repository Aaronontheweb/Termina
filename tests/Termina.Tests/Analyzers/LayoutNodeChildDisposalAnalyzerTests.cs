// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Termina.Generators;
using Termina.Tests.Utility;

namespace Termina.Tests.Analyzers;

using Verify = TerminaVerifier<LayoutNodeChildDisposalAnalyzer>;

/// <summary>
/// Tests for <see cref="LayoutNodeChildDisposalAnalyzer"/> (TERMINA003).
///
/// SuccessCases are the happy path (correct code, no diagnostic). FailureCases are the sad path (a container
/// that disposes a layout node child outside its own Dispose() method). Each failure case marks the expected
/// location with <c>{|#0:Dispose|}</c> markup and passes the field name as the expected message argument.
/// </summary>
public sealed class LayoutNodeChildDisposalAnalyzerTests
{
    private const string TerminaFrameworkSource = """
        namespace Termina.Layout
        {
            public interface ILayoutNode : System.IDisposable { }

            public interface IActivatableNode : ILayoutNode
            {
                void OnActivate();
                void OnDeactivate();
            }

            public abstract class LayoutNode : ILayoutNode
            {
                public virtual void Dispose() { }
            }

            public sealed class TextNode : LayoutNode
            {
                public TextNode(string text) { }
            }

            public sealed class ContentNode : LayoutNode, IActivatableNode
            {
                public void OnActivate() { }
                public void OnDeactivate() { }
            }

            public sealed class FakeObservable
            {
                public System.IDisposable Subscribe(System.Action<object> onNext) => null!;
            }
        }
        """;

    // -------------------------------------------------------------------------------------------------
    // Happy path: correct code that keeps children alive, or disposes only during teardown. No warning.
    // -------------------------------------------------------------------------------------------------
    public static readonly TheoryData<string> SuccessCases = new()
    {
        // N1: dispose the child inside the container's own Dispose() override (final teardown).
        """
        using Termina.Layout;

        public sealed class MyContainer : LayoutNode
        {
            private ContentNode? _currentChild;

            public override void Dispose()
            {
                _currentChild?.Dispose();
                base.Dispose();
            }
        }
        """,

        // N2: dispose the child inside DisposeAsync() (also final teardown).
        """
        using Termina.Layout;

        public sealed class MyContainer : LayoutNode
        {
            private ContentNode? _currentChild;

            public System.Threading.Tasks.ValueTask DisposeAsync()
            {
                _currentChild?.Dispose();
                return default;
            }
        }
        """,

        // N3: deactivate the old child on a switch. There is no Dispose() call.
        """
        using Termina.Layout;

        public sealed class MyContainer : LayoutNode
        {
            private ContentNode? _currentChild;

            public void SwitchTo(ContentNode next)
            {
                _currentChild?.OnDeactivate();
                _currentChild = next;
            }
        }
        """,

        // N4: dispose a non-layout IDisposable (a subscription). Not an ILayoutNode.
        """
        using Termina.Layout;

        public sealed class MyContainer : LayoutNode
        {
            private System.IDisposable? _subscription;

            public void Refresh()
            {
                _subscription?.Dispose();
            }
        }
        """,

        // N5: the enclosing type is not a LayoutNode (for example a page). Out of scope.
        """
        using Termina.Layout;

        public sealed class MyPage
        {
            private ContentNode? _child;

            public void Close()
            {
                _child?.Dispose();
            }
        }
        """,

        // N6: dispose a local layout node, not a field or property. Out of scope.
        """
        using Termina.Layout;

        public sealed class MyContainer : LayoutNode
        {
            public void Rebuild()
            {
                var temp = new ContentNode();
                temp.Dispose();
            }
        }
        """,
    };

    // -------------------------------------------------------------------------------------------------
    // Sad path: a LayoutNode container disposes a layout node child outside its own Dispose(). One warning.
    // Tuple: (source with {|#0:Dispose|} markup, disposed field name).
    // -------------------------------------------------------------------------------------------------
    public static readonly TheoryData<string, string> FailureCases = new()
    {
        // S1: dispose the child in a content-switch method.
        {
            """
            using Termina.Layout;

            public sealed class MyContainer : LayoutNode
            {
                private ContentNode? _currentChild;

                public void SwitchTo(ContentNode next)
                {
                    _currentChild?.{|#0:Dispose|}();
                    _currentChild = next;
                }
            }
            """,
            "_currentChild"
        },

        // S2: dispose the child inside a Subscribe(...) handler (the shape of #70).
        {
            """
            using Termina.Layout;

            public sealed class MyContainer : LayoutNode
            {
                private ILayoutNode? _currentChild;
                private FakeObservable _source = new FakeObservable();

                public MyContainer()
                {
                    _source.Subscribe(v =>
                    {
                        _currentChild?.{|#0:Dispose|}();
                        _currentChild = new TextNode("next");
                    });
                }
            }
            """,
            "_currentChild"
        },

        // S3: dispose the old child, then assign the new one, via a plain (non-conditional) call.
        {
            """
            using Termina.Layout;

            public sealed class MyContainer : LayoutNode
            {
                private ContentNode _currentChild = new ContentNode();

                public void Replace(ContentNode next)
                {
                    _currentChild.{|#0:Dispose|}();
                    _currentChild = next;
                }
            }
            """,
            "_currentChild"
        },

        // S4: dispose a layout node exposed as a property.
        {
            """
            using Termina.Layout;

            public sealed class MyContainer : LayoutNode
            {
                private ContentNode? Current { get; set; }

                public void Clear()
                {
                    Current?.{|#0:Dispose|}();
                    Current = null;
                }
            }
            """,
            "Current"
        },
    };

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public Task SuccessCase(string testCode)
        => Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);

    [Theory]
    [MemberData(nameof(FailureCases))]
    public Task FailureCase(string testCode, string memberName)
        => Verify.VerifyAnalyzer(
            [TerminaFrameworkSource, testCode],
            Verify.Diagnostic(LayoutNodeChildDisposalAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(memberName)
                .WithSeverity(DiagnosticSeverity.Warning));
}

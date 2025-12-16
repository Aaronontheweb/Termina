// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Termina.Generators;
using Termina.Tests.Utility;

namespace Termina.Tests.Analyzers;

using Verify = TerminaVerifier<LayoutNodeInViewModelAnalyzer>;

/// <summary>
/// Tests for <see cref="LayoutNodeInViewModelAnalyzer"/>.
/// Verifies that layout nodes declared in ViewModels are flagged as errors.
/// </summary>
public class LayoutNodeInViewModelAnalyzerTests
{
    // Common source code for ReactiveViewModel and ILayoutNode that all tests need
    private const string TerminaFrameworkSource = """
        namespace Termina.Reactive
        {
            public abstract class ReactiveViewModel : System.IDisposable
            {
                public virtual void Dispose() { }
            }

            [System.AttributeUsage(System.AttributeTargets.Field)]
            public class ReactiveAttribute : System.Attribute { }
        }

        namespace Termina.Layout
        {
            public interface ILayoutNode { }

            public abstract class LayoutNode : ILayoutNode { }

            public abstract class ContainerNode : LayoutNode { }

            public sealed class TextNode : LayoutNode
            {
                public TextNode(string text) { }
            }

            public sealed class StreamingTextNode : LayoutNode
            {
                public static StreamingTextNode Create() => new();
            }

            public sealed class TextInputNode : LayoutNode { }

            public sealed class SpinnerNode : LayoutNode { }

            public sealed class ModalNode : ILayoutNode { }

            public sealed class PanelNode : LayoutNode { }

            public sealed class HorizontalLayout : ContainerNode { }

            public sealed class VerticalLayout : ContainerNode { }

            public sealed class SelectionListNode<T> : ILayoutNode { }

            public sealed class ScrollableContainerNode : LayoutNode { }

            public sealed class ConditionalNode : LayoutNode { }

            public sealed class EmptyNode : LayoutNode { }
        }
        """;

    #region Test Data

    /// <summary>
    /// Test cases that should NOT produce any diagnostics (valid code).
    /// </summary>
    public static readonly TheoryData<string> SuccessCases = new()
    {
        // Reactive properties are fine
        """
        using Termina.Reactive;

        public partial class TestViewModel : ReactiveViewModel
        {
            [Reactive] private string _message = "";
            [Reactive] private int _count;
        }
        """,

        // Primitive properties are fine
        """
        using Termina.Reactive;
        using System.Collections.Generic;

        public partial class TestViewModel : ReactiveViewModel
        {
            public string Name { get; }
            public int Count { get; }
            public List<string> Items { get; }
        }
        """,

        // Layout nodes in non-ViewModel classes are fine
        """
        using Termina.Layout;

        public class RegularClass
        {
            public TextNode MyText { get; }
        }
        """,

        // Layout nodes in Page classes are fine
        """
        using Termina.Layout;

        public class TestPage
        {
            private TextNode _title;
            public ILayoutNode BuildLayout() => _title;
        }
        """,

        // Non-ILayoutNode types with "Node" suffix are fine
        """
        using Termina.Reactive;

        public class TreeNode { }

        public partial class TestViewModel : ReactiveViewModel
        {
            public TreeNode MyTree { get; }
        }
        """,

        // Nested class inside ViewModel that isn't a ViewModel itself is fine
        """
        using Termina.Reactive;
        using Termina.Layout;

        public partial class OuterViewModel : ReactiveViewModel
        {
            public class Inner
            {
                public TextNode InnerNode { get; }
            }
        }
        """,

        // Local variables in ViewModel methods are fine
        """
        using Termina.Reactive;
        using Termina.Layout;

        public partial class TestViewModel : ReactiveViewModel
        {
            public void SomeMethod()
            {
                var text = new TextNode("Hello");
            }
        }
        """
    };

    /// <summary>
    /// Test cases that SHOULD produce diagnostics (invalid code).
    /// Each tuple contains: (testCode, expectedPropertyName, expectedTypeName, expectedLine)
    /// </summary>
    public static TheoryData<string, string, string, int> FailureCases
    {
        get
        {
            var data = new TheoryData<string, string, string, int>();

            // TextNode property
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class TestViewModel : ReactiveViewModel
                {
                    public TextNode MyText { get; }
                }
                """,
                "MyText",
                "TextNode",
                6);

            // StreamingTextNode property with initializer
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class ChatViewModel : ReactiveViewModel
                {
                    public StreamingTextNode ChatHistory { get; } = StreamingTextNode.Create();
                }
                """,
                "ChatHistory",
                "StreamingTextNode",
                6);

            // TextInputNode property
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class InputViewModel : ReactiveViewModel
                {
                    public TextInputNode PromptInput { get; }
                }
                """,
                "PromptInput",
                "TextInputNode",
                6);

            // Generic SelectionListNode property
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class ListViewModel : ReactiveViewModel
                {
                    public SelectionListNode<string> Items { get; }
                }
                """,
                "Items",
                "SelectionListNode<string>",
                6);

            // Nullable ModalNode property
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class TestViewModel : ReactiveViewModel
                {
                    public ModalNode? Dialog { get; set; }
                }
                """,
                "Dialog",
                "ModalNode?",
                6);

            // HorizontalLayout (container) property
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class TestViewModel : ReactiveViewModel
                {
                    public HorizontalLayout Layout { get; }
                }
                """,
                "Layout",
                "HorizontalLayout",
                6);

            // Derived ViewModel with TextNode (property on line 8 due to extra BaseViewModel class)
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public abstract class BaseViewModel : ReactiveViewModel { }

                public partial class DerivedViewModel : BaseViewModel
                {
                    public TextNode Text { get; }
                }
                """,
                "Text",
                "TextNode",
                8);

            return data;
        }
    }

    /// <summary>
    /// Test cases for fields that SHOULD produce diagnostics.
    /// Each tuple contains: (testCode, expectedFieldName, expectedTypeName, expectedLine)
    /// </summary>
    public static TheoryData<string, string, string, int> FieldFailureCases
    {
        get
        {
            var data = new TheoryData<string, string, string, int>();

            // Nullable ModalNode field
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class TestViewModel : ReactiveViewModel
                {
                    private ModalNode? _modal;
                }
                """,
                "_modal",
                "ModalNode?",
                6);

            // TextNode field with initializer
            data.Add(
                """
                using Termina.Reactive;
                using Termina.Layout;

                public partial class TestViewModel : ReactiveViewModel
                {
                    private readonly TextNode _title = new TextNode("Title");
                }
                """,
                "_title",
                "TextNode",
                6);

            return data;
        }
    }

    #endregion

    #region Success Tests

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public async Task SuccessCase_NoErrorReported(string testCode)
    {
        // These should produce no diagnostics
        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode]);
    }

    #endregion

    #region Property Failure Tests

    [Theory]
    [MemberData(nameof(FailureCases))]
    public async Task PropertyFailureCase_ErrorReported(string testCode, string expectedName, string expectedType, int expectedLine)
    {
        // Test1.cs because TerminaFrameworkSource is Test0.cs
        var expected = Verify.Diagnostic()
            .WithLocation("/0/Test1.cs", expectedLine, 5)
            .WithArguments(expectedName, expectedType)
            .WithSeverity(DiagnosticSeverity.Error);

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], expected);
    }

    #endregion

    #region Field Failure Tests

    [Theory]
    [MemberData(nameof(FieldFailureCases))]
    public async Task FieldFailureCase_ErrorReported(string testCode, string expectedName, string expectedType, int expectedLine)
    {
        // Test1.cs because TerminaFrameworkSource is Test0.cs
        var expected = Verify.Diagnostic()
            .WithLocation("/0/Test1.cs", expectedLine, 5)
            .WithArguments(expectedName, expectedType)
            .WithSeverity(DiagnosticSeverity.Error);

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], expected);
    }

    #endregion

    #region Multiple Diagnostics Test

    [Fact]
    public async Task MultipleLayoutNodeProperties_ReportsMultipleErrors()
    {
        const string testCode = """
            using Termina.Reactive;
            using Termina.Layout;

            public partial class TestViewModel : ReactiveViewModel
            {
                public TextNode Title { get; }
                public StreamingTextNode Content { get; }
                public SpinnerNode Loader { get; }
            }
            """;

        var expected1 = Verify.Diagnostic()
            .WithLocation("/0/Test1.cs", 6, 5)
            .WithArguments("Title", "TextNode")
            .WithSeverity(DiagnosticSeverity.Error);

        var expected2 = Verify.Diagnostic()
            .WithLocation("/0/Test1.cs", 7, 5)
            .WithArguments("Content", "StreamingTextNode")
            .WithSeverity(DiagnosticSeverity.Error);

        var expected3 = Verify.Diagnostic()
            .WithLocation("/0/Test1.cs", 8, 5)
            .WithArguments("Loader", "SpinnerNode")
            .WithSeverity(DiagnosticSeverity.Error);

        await Verify.VerifyAnalyzer([TerminaFrameworkSource, testCode], expected1, expected2, expected3);
    }

    #endregion
}

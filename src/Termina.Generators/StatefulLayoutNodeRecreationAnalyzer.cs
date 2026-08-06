// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Termina.Generators;

/// <summary>
/// Detects broad invalidation of dynamic layout nodes whose factory mutates stateful layout node fields.
///
/// DynamicLayoutNode re-evaluates its factory on Invalidate(). If that factory clears or recreates
/// field-backed stateful controls (for example ScrollableContainerNode), the control loses state such as
/// scroll offset, selected index, input text, or cursor position. Prefer invalidating the nested dynamic
/// child that renders changed data, or use KeyedDynamicLayoutNode when switching by state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StatefulLayoutNodeRecreationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TERMINA003";

    private const string Category = "Termina.State";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Avoid invalidating dynamic layouts that recreate stateful nodes",
        messageFormat: "Dynamic layout field '{0}' rebuilds stateful layout node field '{1}'. Calling Invalidate() can reset UI state such as scroll position; invalidate a narrower child node or preserve the stateful node instance.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Stateful layout nodes such as ScrollableContainerNode own UI state. Recreating them from a DynamicLayoutNode factory during broad invalidation resets that state.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var terminaContext = new TerminaContext(compilationContext.Compilation);
            if (!terminaContext.HasRequiredTypes)
                return;

            compilationContext.RegisterSyntaxNodeAction(
                ctx => AnalyzeInvalidateInvocation(ctx, terminaContext),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvalidateInvocation(SyntaxNodeAnalysisContext context, TerminaContext terminaContext)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.ValueText != "Invalidate")
            return;

        var method = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol as IMethodSymbol;
        if (method is null || !terminaContext.IsDynamicLayoutInvalidate(method))
            return;

        var receiver = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol as IFieldSymbol;
        if (receiver is null || !terminaContext.IsDynamicLayoutNode(receiver.Type))
            return;

        var containingTypeSyntax = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingTypeSyntax is null)
            return;

        var containingType = context.SemanticModel.GetDeclaredSymbol(containingTypeSyntax, context.CancellationToken);
        if (containingType is null)
            return;

        var statefulFields = GetStatefulFields(containingType, terminaContext);
        if (statefulFields.Count == 0)
            return;

        var rebuiltField = FindStatefulFieldRebuiltByDynamicFactory(
            containingTypeSyntax,
            receiver,
            statefulFields,
            context.SemanticModel,
            terminaContext,
            context.CancellationToken);

        if (rebuiltField is null)
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            memberAccess.Name.GetLocation(),
            receiver.Name,
            rebuiltField.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static ImmutableHashSet<IFieldSymbol> GetStatefulFields(
        INamedTypeSymbol containingType,
        TerminaContext terminaContext)
    {
        var builder = ImmutableHashSet.CreateBuilder<IFieldSymbol>(SymbolEqualityComparer.Default);

        foreach (var member in containingType.GetMembers())
        {
            if (member is IFieldSymbol field && terminaContext.IsStatefulLayoutNode(field.Type))
                builder.Add(field);
        }

        return builder.ToImmutable();
    }

    private static IFieldSymbol? FindStatefulFieldRebuiltByDynamicFactory(
        TypeDeclarationSyntax containingTypeSyntax,
        IFieldSymbol dynamicLayoutField,
        ImmutableHashSet<IFieldSymbol> statefulFields,
        SemanticModel semanticModel,
        TerminaContext terminaContext,
        CancellationToken cancellationToken)
    {
        foreach (var assignment in containingTypeSyntax.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var assignedField = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol as IFieldSymbol;
            if (!SymbolEqualityComparer.Default.Equals(assignedField, dynamicLayoutField))
                continue;

            if (assignment.Right is not ObjectCreationExpressionSyntax creation)
                continue;

            var createdType = semanticModel.GetTypeInfo(creation, cancellationToken).Type;
            if (createdType is null || !terminaContext.IsDynamicLayoutNode(createdType))
                continue;

            var factory = creation.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
            if (factory is null)
                continue;

            var rebuiltField = FindStatefulFieldMutation(factory, statefulFields, semanticModel, cancellationToken);
            if (rebuiltField is not null)
                return rebuiltField;
        }

        return null;
    }

    private static IFieldSymbol? FindStatefulFieldMutation(
        ExpressionSyntax factoryExpression,
        ImmutableHashSet<IFieldSymbol> statefulFields,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var body = factoryExpression switch
        {
            ParenthesizedLambdaExpressionSyntax lambda => (SyntaxNode?)lambda.Body,
            SimpleLambdaExpressionSyntax lambda => lambda.Body,
            AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.Body,
            _ => null
        };

        if (body is null)
            return null;

        foreach (var assignment in body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var field = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol as IFieldSymbol;
            if (field is not null && statefulFields.Contains(field))
                return field;
        }

        return null;
    }

    private sealed class TerminaContext
    {
        private readonly INamedTypeSymbol? _dynamicLayoutNode;
        private readonly INamedTypeSymbol? _scrollableContainerNode;
        private readonly INamedTypeSymbol? _selectionListNode;
        private readonly INamedTypeSymbol? _textInputNode;
        private readonly INamedTypeSymbol? _streamingTextNode;

        public TerminaContext(Compilation compilation)
        {
            _dynamicLayoutNode = compilation.GetTypeByMetadataName("Termina.Layout.DynamicLayoutNode");
            _scrollableContainerNode = compilation.GetTypeByMetadataName("Termina.Layout.ScrollableContainerNode");
            _selectionListNode = compilation.GetTypeByMetadataName("Termina.Layout.SelectionListNode`1");
            _textInputNode = compilation.GetTypeByMetadataName("Termina.Layout.TextInputNode");
            _streamingTextNode = compilation.GetTypeByMetadataName("Termina.Layout.StreamingTextNode");
        }

        public bool HasRequiredTypes => _dynamicLayoutNode is not null;

        public bool IsDynamicLayoutInvalidate(IMethodSymbol method)
        {
            return method.Name == "Invalidate"
                && method.Parameters.Length == 0
                && IsDynamicLayoutNode(method.ContainingType);
        }

        public bool IsDynamicLayoutNode(ITypeSymbol type)
        {
            return _dynamicLayoutNode is not null
                && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _dynamicLayoutNode);
        }

        public bool IsStatefulLayoutNode(ITypeSymbol type)
        {
            var original = type.OriginalDefinition;
            return SymbolEquals(original, _scrollableContainerNode)
                || SymbolEquals(original, _selectionListNode)
                || SymbolEquals(original, _textInputNode)
                || SymbolEquals(original, _streamingTextNode);
        }

        private static bool SymbolEquals(ISymbol? left, ISymbol? right)
            => left is not null && right is not null && SymbolEqualityComparer.Default.Equals(left, right);
    }
}

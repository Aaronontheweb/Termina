// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Termina.Generators;

/// <summary>
/// Detects disposal of a layout node child during a content switch.
///
/// Termina layout nodes use an active/inactive lifecycle. <c>IActivatableNode.OnDeactivate()</c> pauses a
/// child and keeps it alive. <c>Dispose()</c> destroys it, so it can no longer render or handle input. A
/// container that swaps content should deactivate the old child, not dispose it. Disposal is only correct in
/// the container's own <c>Dispose()</c> method (final teardown).
///
/// To keep the false-positive rate low, the analyzer only flags <c>Dispose()</c> on a field or property of
/// layout node type, inside a type that derives from <c>LayoutNode</c>, and never inside that type's own
/// <c>Dispose()</c> or <c>DisposeAsync()</c> method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LayoutNodeChildDisposalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TERMINA003";

    private const string Category = "Termina.Lifecycle";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Do not dispose a layout node child when switching content",
        messageFormat: "The layout node '{0}' is disposed outside this type's Dispose() method. Dispose() destroys the node, so it can no longer render or handle input. To switch content, call OnDeactivate() instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Termina layout nodes use an active/inactive lifecycle. OnDeactivate() pauses a child and keeps it alive. Dispose() destroys it. A container should deactivate a switched-out child, not dispose it. Dispose a child only in the container's own Dispose() method.");

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
                ctx => AnalyzeDisposeInvocation(ctx, terminaContext),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeDisposeInvocation(SyntaxNodeAnalysisContext context, TerminaContext terminaContext)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!TryGetDisposeInvocation(invocation, context.SemanticModel, context.CancellationToken, out var disposeName, out var receiverExpression))
            return;

        // The receiver must be a field or property of layout node type (persisted content).
        var receiverSymbol = context.SemanticModel.GetSymbolInfo(receiverExpression, context.CancellationToken).Symbol;
        var receiverType = receiverSymbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };
        if (receiverType is null || !terminaContext.ImplementsLayoutNode(receiverType))
            return;

        // The enclosing type must be a container that derives from LayoutNode.
        var enclosingTypeSyntax = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (enclosingTypeSyntax is null)
            return;

        var enclosingType = context.SemanticModel.GetDeclaredSymbol(enclosingTypeSyntax, context.CancellationToken);
        if (enclosingType is null || !terminaContext.DerivesFromLayoutNode(enclosingType))
            return;

        // Disposal is correct in the container's own teardown, so skip Dispose()/DisposeAsync().
        if (IsInsideDisposeMethod(invocation))
            return;

        var diagnostic = Diagnostic.Create(Rule, disposeName.GetLocation(), receiverSymbol!.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool TryGetDisposeInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SimpleNameSyntax disposeName,
        out ExpressionSyntax receiverExpression)
    {
        disposeName = null!;
        receiverExpression = null!;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            disposeName = memberAccess.Name;
            receiverExpression = memberAccess.Expression;
        }
        else if (invocation.Expression is MemberBindingExpressionSyntax memberBinding
            && invocation.Parent is ConditionalAccessExpressionSyntax conditionalAccess)
        {
            disposeName = memberBinding.Name;
            receiverExpression = conditionalAccess.Expression;
        }
        else
        {
            return false;
        }

        if (disposeName.Identifier.ValueText != "Dispose")
            return false;

        var method = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        return method is { Name: "Dispose", Parameters.Length: 0 };
    }

    /// <summary>
    /// True when the invocation is inside the nearest enclosing <c>Dispose()</c> or <c>DisposeAsync()</c>
    /// method declaration. Disposal there is the container's final teardown, which is correct.
    /// </summary>
    private static bool IsInsideDisposeMethod(SyntaxNode node)
    {
        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        return method is not null
            && method.Identifier.ValueText is "Dispose" or "DisposeAsync";
    }

    private sealed class TerminaContext
    {
        private readonly INamedTypeSymbol? _iLayoutNode;
        private readonly INamedTypeSymbol? _layoutNode;

        public TerminaContext(Compilation compilation)
        {
            _iLayoutNode = compilation.GetTypeByMetadataName("Termina.Layout.ILayoutNode");
            _layoutNode = compilation.GetTypeByMetadataName("Termina.Layout.LayoutNode");
        }

        public bool HasRequiredTypes => _iLayoutNode is not null && _layoutNode is not null;

        public bool ImplementsLayoutNode(ITypeSymbol type)
        {
            if (_iLayoutNode is null)
                return false;

            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _iLayoutNode))
                return true;

            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, _iLayoutNode))
                    return true;
            }

            return false;
        }

        public bool DerivesFromLayoutNode(INamedTypeSymbol type)
        {
            if (_layoutNode is null)
                return false;

            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, _layoutNode))
                    return true;
            }

            return false;
        }
    }
}

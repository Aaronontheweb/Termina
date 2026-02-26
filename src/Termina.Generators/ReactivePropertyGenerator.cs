using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Termina.Generators;

/// <summary>
/// Source generator that generates route parameter receivers for fields marked with [FromRoute].
///
/// For each [FromRoute] field, generates:
/// - A public read-only property
/// - IRouteParameterReceiver implementation with SetRouteParameters method
/// </summary>
[Generator]
public class ReactivePropertyGenerator : IIncrementalGenerator
{
    private const string FromRouteAttributeName = "FromRoute";
    private const string FromRouteAttributeFullName = "Termina.Routing.FromRouteAttribute";

    /// <summary>
    /// Symbol display format that includes nullable reference type annotations (e.g., string? instead of string).
    /// Based on FullyQualifiedFormat but with IncludeNullableReferenceTypeModifier added.
    /// </summary>
    private static readonly SymbolDisplayFormat FullyQualifiedFormatWithNullability = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                              SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all field declarations with [FromRoute] attribute
        var fieldsWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsFieldWithFromRouteAttribute(node),
                transform: static (ctx, _) => GetFieldInfo(ctx))
            .Where(static f => f is not null);

        // Collect and generate
        context.RegisterSourceOutput(
            fieldsWithAttribute.Collect(),
            static (spc, fields) => GenerateSource(spc, fields!));
    }

    private static bool IsFieldWithFromRouteAttribute(SyntaxNode node)
    {
        // Look for field declarations
        if (node is not FieldDeclarationSyntax fieldDeclaration)
            return false;

        // Check if any attribute list contains [FromRoute]
        foreach (var attributeList in fieldDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name == FromRouteAttributeName || name == "FromRouteAttribute" ||
                    name == "Termina.Routing.FromRoute" || name == FromRouteAttributeFullName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static FieldInfo? GetFieldInfo(GeneratorSyntaxContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        // Get the containing type
        if (fieldDeclaration.Parent is not TypeDeclarationSyntax typeDeclaration)
            return null;

        // Check that the containing type is partial
        var isPartial = typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            // Skip - class must be partial for generated code to work
            return null;
        }

        // Get the semantic model
        var semanticModel = context.SemanticModel;

        // Get the field symbol for the first variable (we handle one field per declaration)
        var variableDeclarator = fieldDeclaration.Declaration.Variables.FirstOrDefault();
        if (variableDeclarator == null)
            return null;

        var fieldSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator) as IFieldSymbol;
        if (fieldSymbol == null)
            return null;

        // Check which attributes the field has
        var attributes = fieldSymbol.GetAttributes();
        var fromRouteAttribute = attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == FromRouteAttributeFullName);
        var hasFromRouteAttribute = fromRouteAttribute != null;

        // Must have the attribute
        if (!hasFromRouteAttribute)
            return null;

        // Get the explicit route parameter name if specified
        string? routeParameterName = null;
        if (fromRouteAttribute != null)
        {
            var nameArg = fromRouteAttribute.NamedArguments.FirstOrDefault(a => a.Key == "Name");
            if (!nameArg.Value.IsNull)
            {
                routeParameterName = nameArg.Value.Value as string;
            }
        }

        // Get field name and type (including nullability annotations)
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type.ToDisplayString(FullyQualifiedFormatWithNullability);

        // Get containing type info
        var containingType = fieldSymbol.ContainingType;
        var namespaceName = containingType.ContainingNamespace.IsGlobalNamespace
            ? null
            : containingType.ContainingNamespace.ToDisplayString();
        var typeName = containingType.Name;

        return new FieldInfo(
            namespaceName,
            typeName,
            fieldName,
            fieldType,
            containingType.ToDisplayString(),
            routeParameterName);
    }

    private static void GenerateSource(SourceProductionContext context, ImmutableArray<FieldInfo?> fields)
    {
        // Group fields by containing type
        var fieldsByType = fields
            .Where(f => f is not null)
            .Cast<FieldInfo>()
            .GroupBy(f => f.FullTypeName);

        foreach (var group in fieldsByType)
        {
            var fieldList = group.ToList();
            var firstField = fieldList.First();

            var source = GeneratePartialClass(firstField.Namespace, firstField.TypeName, fieldList);
            context.AddSource($"{firstField.TypeName}.Reactive.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GeneratePartialClass(string? namespaceName, string typeName, List<FieldInfo> fields)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Termina.Routing;");
        sb.AppendLine();

        if (namespaceName is not null)
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {typeName} : IRouteParameterReceiver");
        sb.AppendLine("{");

        // Generate route parameter properties (read-only)
        foreach (var field in fields)
        {
            GenerateRouteProperty(sb, field);
        }

        // Generate IRouteParameterReceiver implementation
        GenerateSetRouteParameters(sb, fields);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateRouteProperty(StringBuilder sb, FieldInfo field)
    {
        // Convert field name to property name: _fieldName -> FieldName
        var propertyName = GetPropertyName(field.FieldName);

        sb.AppendLine();
        sb.AppendLine($"    public {field.FieldType} {propertyName} => {field.FieldName};");
    }

    private static void GenerateSetRouteParameters(StringBuilder sb, List<FieldInfo> fields)
    {
        sb.AppendLine();
        sb.AppendLine("    void IRouteParameterReceiver.SetRouteParameters(IReadOnlyDictionary<string, object> parameters)");
        sb.AppendLine("    {");

        foreach (var field in fields)
        {
            // Use explicit name from attribute if provided, otherwise derive from field name
            var parameterName = field.RouteParameterName ?? GetRouteParameterName(field.FieldName);

            sb.AppendLine($"        if (parameters.TryGetValue(\"{parameterName}\", out var {field.FieldName}Value))");
            sb.AppendLine($"            {field.FieldName} = ({field.FieldType}){field.FieldName}Value;");
        }

        sb.AppendLine("    }");
    }

    private static string GetRouteParameterName(string fieldName)
    {
        // Remove leading underscore and use camelCase for route parameter
        // _taskId -> taskId
        if (fieldName.StartsWith("_") && fieldName.Length > 1)
        {
            return char.ToLowerInvariant(fieldName[1]) + fieldName.Substring(2);
        }

        // Just use as-is with lowercase first letter
        return char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    private static string GetPropertyName(string fieldName)
    {
        // Remove leading underscore and capitalize first letter
        if (fieldName.StartsWith("_") && fieldName.Length > 1)
        {
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        }

        // Just capitalize first letter
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    private sealed class FieldInfo
    {
        public string? Namespace { get; }
        public string TypeName { get; }
        public string FieldName { get; }
        public string FieldType { get; }
        public string FullTypeName { get; }
        public string? RouteParameterName { get; }

        public FieldInfo(
            string? @namespace,
            string typeName,
            string fieldName,
            string fieldType,
            string fullTypeName,
            string? routeParameterName)
        {
            Namespace = @namespace;
            TypeName = typeName;
            FieldName = fieldName;
            FieldType = fieldType;
            FullTypeName = fullTypeName;
            RouteParameterName = routeParameterName;
        }
    }
}

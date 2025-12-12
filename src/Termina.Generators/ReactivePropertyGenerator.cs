using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Termina.Generators;

/// <summary>
/// Source generator that generates reactive properties for fields marked with [Reactive].
/// For each [Reactive] field, generates:
/// - A BehaviorSubject backing field
/// - A public property with get/set
/// - A public IObservable property for subscriptions
/// </summary>
[Generator]
public class ReactivePropertyGenerator : IIncrementalGenerator
{
    private const string ReactiveAttributeName = "Reactive";
    private const string ReactiveAttributeFullName = "Termina.Reactive.ReactiveAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all field declarations with [Reactive] attribute
        var fieldsWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsFieldWithReactiveAttribute(node),
                transform: static (ctx, _) => GetFieldInfo(ctx))
            .Where(static f => f is not null);

        // Collect and generate
        context.RegisterSourceOutput(
            fieldsWithAttribute.Collect(),
            static (spc, fields) => GenerateSource(spc, fields!));
    }

    private static bool IsFieldWithReactiveAttribute(SyntaxNode node)
    {
        // Look for field declarations
        if (node is not FieldDeclarationSyntax fieldDeclaration)
            return false;

        // Check if any attribute list contains [Reactive]
        foreach (var attributeList in fieldDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name == ReactiveAttributeName || name == "ReactiveAttribute" ||
                    name == "Termina.Reactive.Reactive" || name == ReactiveAttributeFullName)
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

        // Verify the attribute is actually the Reactive attribute we expect
        var hasReactiveAttribute = fieldSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == ReactiveAttributeFullName);

        if (!hasReactiveAttribute)
            return null;

        // Get field name and type
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Get the initial value if present
        string? initialValue = null;
        if (variableDeclarator.Initializer is not null)
        {
            initialValue = variableDeclarator.Initializer.Value.ToString();
        }

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
            initialValue,
            containingType.ToDisplayString());
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
            var firstField = group.First();
            var source = GeneratePartialClass(firstField.Namespace, firstField.TypeName, group.ToList());
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
        sb.AppendLine("using System.Reactive.Linq;");
        sb.AppendLine("using System.Reactive.Subjects;");
        sb.AppendLine();

        if (namespaceName is not null)
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {typeName}");
        sb.AppendLine("{");

        foreach (var field in fields)
        {
            GenerateProperty(sb, field);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateProperty(StringBuilder sb, FieldInfo field)
    {
        // Convert field name to property name: _fieldName -> FieldName
        var propertyName = GetPropertyName(field.FieldName);
        var subjectFieldName = $"{field.FieldName}Subject";

        // Default value for the BehaviorSubject
        var defaultValue = field.InitialValue ?? GetDefaultValue(field.FieldType);

        sb.AppendLine();
        sb.AppendLine($"    private readonly BehaviorSubject<{field.FieldType}> {subjectFieldName} = new({defaultValue});");
        sb.AppendLine();
        sb.AppendLine($"    public {field.FieldType} {propertyName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {subjectFieldName}.Value;");
        sb.AppendLine($"        set => {subjectFieldName}.OnNext(value);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public IObservable<{field.FieldType}> {propertyName}Changed => {subjectFieldName}.AsObservable();");
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

    private static string GetDefaultValue(string typeName)
    {
        // Handle common types
        return typeName switch
        {
            "int" or "global::System.Int32" => "0",
            "long" or "global::System.Int64" => "0L",
            "float" or "global::System.Single" => "0f",
            "double" or "global::System.Double" => "0d",
            "decimal" or "global::System.Decimal" => "0m",
            "bool" or "global::System.Boolean" => "false",
            "string" or "global::System.String" => "string.Empty",
            _ => "default!"
        };
    }

    private sealed class FieldInfo
    {
        public string? Namespace { get; }
        public string TypeName { get; }
        public string FieldName { get; }
        public string FieldType { get; }
        public string? InitialValue { get; }
        public string FullTypeName { get; }

        public FieldInfo(
            string? @namespace,
            string typeName,
            string fieldName,
            string fieldType,
            string? initialValue,
            string fullTypeName)
        {
            Namespace = @namespace;
            TypeName = typeName;
            FieldName = fieldName;
            FieldType = fieldType;
            InitialValue = initialValue;
            FullTypeName = fullTypeName;
        }
    }
}

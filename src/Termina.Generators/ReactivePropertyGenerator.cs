using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Termina.Generators;

/// <summary>
/// Source generator that generates reactive properties for fields marked with [Reactive]
/// and route parameter receivers for fields marked with [FromRoute].
///
/// For each [Reactive] field, generates:
/// - A BehaviorSubject backing field
/// - A public property with get/set
/// - A public IObservable property for subscriptions
///
/// For each [FromRoute] field, generates:
/// - A public read-only property
/// - IRouteParameterReceiver implementation with SetRouteParameters method
/// </summary>
[Generator]
public class ReactivePropertyGenerator : IIncrementalGenerator
{
    private const string ReactiveAttributeName = "Reactive";
    private const string ReactiveAttributeFullName = "Termina.Reactive.ReactiveAttribute";
    private const string FromRouteAttributeName = "FromRoute";
    private const string FromRouteAttributeFullName = "Termina.Routing.FromRouteAttribute";

    /// <summary>
    /// Diagnostic reported when a ReactiveViewModel subclass has a custom Dispose() method
    /// but needs to call DisposeReactiveFields() to properly dispose generated BehaviorSubjects.
    /// </summary>
    private static readonly DiagnosticDescriptor MustCallDisposeReactiveFields = new(
        id: "TERMINA001",
        title: "Must call DisposeReactiveFields() in custom Dispose() override",
        messageFormat: "Class '{0}' inherits from ReactiveViewModel and has [Reactive] fields, but defines a custom Dispose() method. You must call DisposeReactiveFields() in your Dispose() implementation to properly dispose generated BehaviorSubject backing fields.",
        category: "Termina.Reactive",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When a ReactiveViewModel subclass has [Reactive] fields and defines its own Dispose() method, it must call the generated DisposeReactiveFields() method to ensure BehaviorSubject backing fields are properly disposed.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all field declarations with [Reactive] or [FromRoute] attribute
        var fieldsWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsFieldWithReactiveOrFromRouteAttribute(node),
                transform: static (ctx, _) => GetFieldInfo(ctx))
            .Where(static f => f is not null);

        // Collect and generate
        context.RegisterSourceOutput(
            fieldsWithAttribute.Collect(),
            static (spc, fields) => GenerateSource(spc, fields!));
    }

    private static bool IsFieldWithReactiveOrFromRouteAttribute(SyntaxNode node)
    {
        // Look for field declarations
        if (node is not FieldDeclarationSyntax fieldDeclaration)
            return false;

        // Check if any attribute list contains [Reactive] or [FromRoute]
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
        var hasReactiveAttribute = attributes.Any(a => a.AttributeClass?.ToDisplayString() == ReactiveAttributeFullName);
        var fromRouteAttribute = attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == FromRouteAttributeFullName);
        var hasFromRouteAttribute = fromRouteAttribute != null;

        // Must have at least one of the attributes
        if (!hasReactiveAttribute && !hasFromRouteAttribute)
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

        // Check if the containing type inherits from ReactiveViewModel
        var inheritsFromReactiveViewModel = InheritsFromType(containingType, "Termina.Reactive.ReactiveViewModel");

        // Check if the class already has a Dispose() method defined
        var disposeMethod = containingType.GetMembers("Dispose")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Parameters.Length == 0 &&
                      m.ReturnType.SpecialType == SpecialType.System_Void &&
                      !m.IsImplicitlyDeclared);
        var hasExistingDisposeMethod = disposeMethod is not null;

        // Check if the existing Dispose method calls DisposeReactiveFields()
        var disposeCallsDisposeReactiveFields = false;
        if (disposeMethod is not null)
        {
            // Get the syntax reference for the dispose method
            var syntaxRef = disposeMethod.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef is not null)
            {
                var disposeSyntax = syntaxRef.GetSyntax();
                // Check if the method body contains a call to DisposeReactiveFields
                disposeCallsDisposeReactiveFields = disposeSyntax.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Any(inv =>
                    {
                        // Check for direct call: DisposeReactiveFields()
                        if (inv.Expression is IdentifierNameSyntax id &&
                            id.Identifier.Text == "DisposeReactiveFields")
                            return true;

                        // Check for this.DisposeReactiveFields()
                        if (inv.Expression is MemberAccessExpressionSyntax ma &&
                            ma.Name.Identifier.Text == "DisposeReactiveFields")
                            return true;

                        return false;
                    });
            }
        }

        // Get the location of the type declaration for diagnostics
        var typeLocation = typeDeclaration.Identifier.GetLocation();

        return new FieldInfo(
            namespaceName,
            typeName,
            fieldName,
            fieldType,
            initialValue,
            containingType.ToDisplayString(),
            hasReactiveAttribute,
            hasFromRouteAttribute,
            routeParameterName,
            inheritsFromReactiveViewModel,
            hasExistingDisposeMethod,
            disposeCallsDisposeReactiveFields,
            typeLocation);
    }

    private static bool InheritsFromType(INamedTypeSymbol? type, string baseTypeName)
    {
        var current = type?.BaseType;
        while (current != null)
        {
            // Check against multiple possible display formats
            var displayString = current.ToDisplayString();
            if (displayString == baseTypeName)
                return true;

            // Also check the fully qualified name (namespace + name)
            var fullyQualifiedName = current.ContainingNamespace.IsGlobalNamespace
                ? current.Name
                : $"{current.ContainingNamespace.ToDisplayString()}.{current.Name}";
            if (fullyQualifiedName == baseTypeName)
                return true;

            // Also check just by type name and namespace separately (for metadata types)
            if (current.Name == "ReactiveViewModel" &&
                current.ContainingNamespace.ToDisplayString() == "Termina.Reactive")
                return true;

            current = current.BaseType;
        }
        return false;
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
            var reactiveFields = fieldList.Where(f => f.IsReactive).ToList();

            // Report diagnostic if user has custom Dispose() but doesn't call DisposeReactiveFields()
            if (reactiveFields.Count > 0 &&
                firstField.InheritsFromReactiveViewModel &&
                firstField.HasExistingDisposeMethod &&
                !firstField.DisposeCallsDisposeReactiveFields &&
                firstField.TypeLocation is not null)
            {
                var diagnostic = Diagnostic.Create(
                    MustCallDisposeReactiveFields,
                    firstField.TypeLocation,
                    firstField.TypeName);
                context.ReportDiagnostic(diagnostic);
            }

            var source = GeneratePartialClass(firstField.Namespace, firstField.TypeName, fieldList);
            context.AddSource($"{firstField.TypeName}.Reactive.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GeneratePartialClass(string? namespaceName, string typeName, List<FieldInfo> fields)
    {
        var sb = new StringBuilder();
        var reactiveFields = fields.Where(f => f.IsReactive).ToList();
        var fromRouteFields = fields.Where(f => f.IsFromRoute).ToList();
        var hasFromRouteFields = fromRouteFields.Count > 0;

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        if (reactiveFields.Count > 0)
        {
            sb.AppendLine("using System.Reactive.Linq;");
            sb.AppendLine("using System.Reactive.Subjects;");
        }
        if (hasFromRouteFields)
        {
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Termina.Routing;");
        }
        sb.AppendLine();

        if (namespaceName is not null)
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        // Add IRouteParameterReceiver interface if needed
        if (hasFromRouteFields)
        {
            sb.AppendLine($"partial class {typeName} : IRouteParameterReceiver");
        }
        else
        {
            sb.AppendLine($"partial class {typeName}");
        }
        sb.AppendLine("{");

        // Generate reactive properties
        foreach (var field in reactiveFields)
        {
            GenerateReactiveProperty(sb, field);
        }

        // Generate route parameter properties (read-only)
        foreach (var field in fromRouteFields)
        {
            GenerateRouteProperty(sb, field);
        }

        // Generate IRouteParameterReceiver implementation if needed
        if (hasFromRouteFields)
        {
            GenerateSetRouteParameters(sb, fromRouteFields);
        }

        // Generate disposal method for reactive fields
        if (reactiveFields.Count > 0)
        {
            GenerateDisposeReactiveFields(sb, reactiveFields);

            // Generate Dispose() override for ReactiveViewModel subclasses
            // (only if user hasn't already defined one)
            var firstField = reactiveFields.First();
            if (firstField.InheritsFromReactiveViewModel && !firstField.HasExistingDisposeMethod)
            {
                GenerateDisposeOverride(sb);
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateReactiveProperty(StringBuilder sb, FieldInfo field)
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

    private static void GenerateRouteProperty(StringBuilder sb, FieldInfo field)
    {
        // Convert field name to property name: _fieldName -> FieldName
        var propertyName = GetPropertyName(field.FieldName);

        sb.AppendLine();
        sb.AppendLine($"    public {field.FieldType} {propertyName} => {field.FieldName};");
    }

    private static void GenerateSetRouteParameters(StringBuilder sb, List<FieldInfo> fromRouteFields)
    {
        sb.AppendLine();
        sb.AppendLine("    void IRouteParameterReceiver.SetRouteParameters(IReadOnlyDictionary<string, object> parameters)");
        sb.AppendLine("    {");

        foreach (var field in fromRouteFields)
        {
            var propertyName = GetPropertyName(field.FieldName);
            // Use explicit name from attribute if provided, otherwise derive from field name
            var parameterName = field.RouteParameterName ?? GetRouteParameterName(field.FieldName);

            sb.AppendLine($"        if (parameters.TryGetValue(\"{parameterName}\", out var {field.FieldName}Value))");
            sb.AppendLine($"            {field.FieldName} = ({field.FieldType}){field.FieldName}Value;");
        }

        sb.AppendLine("    }");
    }

    private static void GenerateDisposeReactiveFields(StringBuilder sb, List<FieldInfo> reactiveFields)
    {
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Disposes all BehaviorSubject backing fields generated by [Reactive] attributes.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    protected void DisposeReactiveFields()");
        sb.AppendLine("    {");

        foreach (var field in reactiveFields)
        {
            var subjectFieldName = $"{field.FieldName}Subject";
            sb.AppendLine($"        {subjectFieldName}.Dispose();");
        }

        sb.AppendLine("    }");
    }

    private static void GenerateDisposeOverride(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Disposes reactive property backing fields and calls base disposal.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override void Dispose()");
        sb.AppendLine("    {");
        sb.AppendLine("        DisposeReactiveFields();");
        sb.AppendLine("        base.Dispose();");
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
        public bool IsReactive { get; }
        public bool IsFromRoute { get; }
        public string? RouteParameterName { get; }
        public bool InheritsFromReactiveViewModel { get; }
        public bool HasExistingDisposeMethod { get; }
        public bool DisposeCallsDisposeReactiveFields { get; }
        public Location? TypeLocation { get; }

        public FieldInfo(
            string? @namespace,
            string typeName,
            string fieldName,
            string fieldType,
            string? initialValue,
            string fullTypeName,
            bool isReactive,
            bool isFromRoute,
            string? routeParameterName,
            bool inheritsFromReactiveViewModel,
            bool hasExistingDisposeMethod,
            bool disposeCallsDisposeReactiveFields,
            Location? typeLocation)
        {
            Namespace = @namespace;
            TypeName = typeName;
            FieldName = fieldName;
            FieldType = fieldType;
            InitialValue = initialValue;
            FullTypeName = fullTypeName;
            IsReactive = isReactive;
            IsFromRoute = isFromRoute;
            RouteParameterName = routeParameterName;
            InheritsFromReactiveViewModel = inheritsFromReactiveViewModel;
            HasExistingDisposeMethod = hasExistingDisposeMethod;
            DisposeCallsDisposeReactiveFields = disposeCallsDisposeReactiveFields;
            TypeLocation = typeLocation;
        }
    }
}

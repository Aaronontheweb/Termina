using System.Diagnostics.CodeAnalysis;

namespace Termina.Routing;

/// <summary>
/// Matches incoming paths against registered route templates and extracts parameters.
/// </summary>
public sealed class RouteMatcher
{
    private readonly List<RouteRegistration> _routes = new();

    /// <summary>
    /// Adds a route to the matcher.
    /// </summary>
    /// <param name="template">The route template.</param>
    /// <param name="pageKey">A unique key identifying the page for this route.</param>
    public void AddRoute(RouteTemplate template, string pageKey)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(pageKey);

        _routes.Add(new RouteRegistration(template, pageKey));
    }

    /// <summary>
    /// Attempts to match a path against registered routes.
    /// </summary>
    /// <param name="path">The path to match (e.g., "/tasks/42").</param>
    /// <param name="pageKey">The matched page key if successful.</param>
    /// <param name="parameters">The extracted parameters if successful.</param>
    /// <returns>True if a match was found; false otherwise.</returns>
    public bool TryMatch(string path, out string? pageKey, out IReadOnlyDictionary<string, object>? parameters)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Normalize path
        if (!path.StartsWith('/'))
            path = "/" + path;

        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Try each route - first match wins
        foreach (var route in _routes)
        {
            if (TryMatchRoute(route.Template, pathSegments, out var extractedParams))
            {
                pageKey = route.PageKey;
                parameters = extractedParams;
                return true;
            }
        }

        pageKey = null;
        parameters = null;
        return false;
    }

    /// <summary>
    /// Builds a path from a route template and values.
    /// </summary>
    /// <param name="template">The route template (e.g., "/tasks/{id}").</param>
    /// <param name="values">The parameter values to substitute.</param>
    /// <returns>The built path.</returns>
    public static string BuildPath(string template, object? values)
    {
        if (values == null)
            return template;

        var parsed = RouteParser.Parse(template);
        return BuildPath(parsed, values);
    }

    /// <summary>
    /// Builds a path from a parsed route template and values.
    /// </summary>
    public static string BuildPath(RouteTemplate template, object? values)
    {
        if (values == null || !template.HasParameters)
            return template.Template;

        var valueDict = ObjectToDictionary(values);
        var segments = new List<string>();

        foreach (var segment in template.Segments)
        {
            if (segment.IsParameter)
            {
                if (!valueDict.TryGetValue(segment.Value, out var value))
                    throw new ArgumentException($"Missing value for route parameter '{segment.Value}'.");

                segments.Add(value?.ToString() ?? string.Empty);
            }
            else
            {
                segments.Add(segment.Value);
            }
        }

        return "/" + string.Join("/", segments);
    }

    private static bool TryMatchRoute(
        RouteTemplate template,
        string[] pathSegments,
        out IReadOnlyDictionary<string, object>? parameters)
    {
        parameters = null;

        // Must have same number of segments
        if (template.Segments.Count != pathSegments.Length)
            return false;

        var extractedParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < template.Segments.Count; i++)
        {
            var templateSegment = template.Segments[i];
            var pathSegment = pathSegments[i];

            if (templateSegment.IsParameter)
            {
                // Try to parse according to constraint
                if (!RouteConstraints.TryParse(pathSegment, templateSegment.Constraint, out var parsedValue))
                    return false;

                extractedParams[templateSegment.Value] = parsedValue!;
            }
            else
            {
                // Literal segment - must match exactly (case-insensitive)
                if (!string.Equals(templateSegment.Value, pathSegment, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        parameters = extractedParams;
        return true;
    }

    /// <summary>
    /// Converts an object to a dictionary of property values.
    /// AOT warning suppressed because anonymous types used in navigation have their
    /// properties preserved by the compiler.
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075",
        Justification = "Anonymous types used for route parameters have their properties preserved.")]
    private static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Handle anonymous objects and regular objects via reflection
        var type = obj.GetType();
        foreach (var prop in type.GetProperties())
        {
            result[prop.Name] = prop.GetValue(obj);
        }

        return result;
    }

    private readonly record struct RouteRegistration(RouteTemplate Template, string PageKey);
}

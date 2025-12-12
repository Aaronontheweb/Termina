using System.Text.RegularExpressions;

namespace Termina.Routing;

/// <summary>
/// Parses route template strings into <see cref="RouteTemplate"/> objects.
/// </summary>
public static partial class RouteParser
{
    // Matches parameter segments like {id} or {id:int}
    [GeneratedRegex(@"^\{([a-zA-Z_][a-zA-Z0-9_]*)(?::([a-zA-Z]+))?\}$")]
    private static partial Regex ParameterPattern();

    /// <summary>
    /// Parses a route template string into a <see cref="RouteTemplate"/>.
    /// </summary>
    /// <param name="template">The route template (e.g., "/tasks/{id:int}").</param>
    /// <returns>The parsed route template.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is invalid.</exception>
    public static RouteTemplate Parse(string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Route template cannot be empty.", nameof(template));

        // Normalize: ensure starts with /
        if (!template.StartsWith('/'))
            template = "/" + template;

        // Split into segments (skip empty from leading /)
        var parts = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segments = new List<RouteSegment>(parts.Length);

        foreach (var part in parts)
        {
            var segment = ParseSegment(part, template);
            segments.Add(segment);
        }

        return new RouteTemplate(template, segments);
    }

    private static RouteSegment ParseSegment(string part, string fullTemplate)
    {
        if (string.IsNullOrEmpty(part))
            throw new ArgumentException($"Empty segment in route template: {fullTemplate}");

        // Check if it's a parameter segment
        if (part.StartsWith('{') && part.EndsWith('}'))
        {
            var match = ParameterPattern().Match(part);
            if (!match.Success)
                throw new ArgumentException($"Invalid parameter segment '{part}' in route template: {fullTemplate}");

            var paramName = match.Groups[1].Value;
            var constraint = match.Groups[2].Success ? match.Groups[2].Value : null;

            // Validate constraint
            if (constraint != null && !RouteConstraints.IsValidConstraint(constraint))
                throw new ArgumentException($"Unknown constraint '{constraint}' in route template: {fullTemplate}. Supported: int, guid, bool");

            return RouteSegment.Parameter(paramName, constraint);
        }

        // It's a literal segment - validate it contains no special characters
        if (part.Contains('{') || part.Contains('}'))
            throw new ArgumentException($"Invalid segment '{part}' in route template: {fullTemplate}. Braces must enclose entire parameter.");

        return RouteSegment.Literal(part);
    }

    /// <summary>
    /// Attempts to parse a route template, returning false on failure.
    /// </summary>
    public static bool TryParse(string template, out RouteTemplate? result)
    {
        try
        {
            result = Parse(template);
            return true;
        }
        catch (ArgumentException)
        {
            result = null;
            return false;
        }
    }
}

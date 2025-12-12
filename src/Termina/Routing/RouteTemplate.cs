namespace Termina.Routing;

/// <summary>
/// A parsed route template consisting of segments.
/// </summary>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item>"/tasks" → one literal segment</item>
///   <item>"/tasks/{id}" → two segments: literal "tasks", parameter "id"</item>
///   <item>"/tasks/{id:int}" → two segments: literal "tasks", parameter "id" with int constraint</item>
/// </list>
/// </remarks>
public sealed class RouteTemplate
{
    /// <summary>
    /// The original template string.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// The parsed segments of the route.
    /// </summary>
    public IReadOnlyList<RouteSegment> Segments { get; }

    /// <summary>
    /// Gets the parameter names defined in this template.
    /// </summary>
    public IReadOnlyList<string> ParameterNames { get; }

    internal RouteTemplate(string template, IReadOnlyList<RouteSegment> segments)
    {
        Template = template;
        Segments = segments;
        ParameterNames = segments
            .Where(s => s.IsParameter)
            .Select(s => s.Value)
            .ToList();
    }

    /// <summary>
    /// Gets whether this template has any parameters.
    /// </summary>
    public bool HasParameters => ParameterNames.Count > 0;

    public override string ToString() => Template;
}

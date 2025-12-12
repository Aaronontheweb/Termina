namespace Termina.Routing;

/// <summary>
/// Represents a segment of a route template.
/// </summary>
/// <remarks>
/// A segment can be either:
/// <list type="bullet">
///   <item>A literal segment (e.g., "tasks" in "/tasks/123")</item>
///   <item>A parameter segment with optional type constraint (e.g., "{id:int}")</item>
/// </list>
/// </remarks>
public readonly struct RouteSegment : IEquatable<RouteSegment>
{
    /// <summary>
    /// The value of the segment. For literals, this is the text. For parameters, this is the parameter name.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Whether this segment is a parameter (enclosed in braces).
    /// </summary>
    public bool IsParameter { get; }

    /// <summary>
    /// The type constraint for parameter segments (e.g., "int", "guid", "bool").
    /// Null for literal segments or parameters without constraints (defaults to string).
    /// </summary>
    public string? Constraint { get; }

    /// <summary>
    /// Creates a literal segment.
    /// </summary>
    public static RouteSegment Literal(string value) => new(value, isParameter: false, constraint: null);

    /// <summary>
    /// Creates a parameter segment.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="constraint">Optional type constraint (int, guid, bool, or null for string).</param>
    public static RouteSegment Parameter(string name, string? constraint = null) =>
        new(name, isParameter: true, constraint: constraint);

    private RouteSegment(string value, bool isParameter, string? constraint)
    {
        Value = value;
        IsParameter = isParameter;
        Constraint = constraint;
    }

    public bool Equals(RouteSegment other) =>
        Value == other.Value && IsParameter == other.IsParameter && Constraint == other.Constraint;

    public override bool Equals(object? obj) => obj is RouteSegment other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, IsParameter, Constraint);

    public override string ToString() =>
        IsParameter
            ? Constraint != null ? $"{{{Value}:{Constraint}}}" : $"{{{Value}}}"
            : Value;

    public static bool operator ==(RouteSegment left, RouteSegment right) => left.Equals(right);
    public static bool operator !=(RouteSegment left, RouteSegment right) => !left.Equals(right);
}

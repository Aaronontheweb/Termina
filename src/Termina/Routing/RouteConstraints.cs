namespace Termina.Routing;

/// <summary>
/// Handles route parameter type constraint validation and parsing.
/// </summary>
public static class RouteConstraints
{
    /// <summary>
    /// The set of valid constraint names.
    /// </summary>
    public static readonly IReadOnlySet<string> ValidConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "int",
        "guid",
        "bool"
    };

    /// <summary>
    /// Checks if the given constraint name is valid.
    /// </summary>
    public static bool IsValidConstraint(string constraint) =>
        ValidConstraints.Contains(constraint);

    /// <summary>
    /// Attempts to parse a string value according to the specified constraint.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="constraint">The constraint type (null means string, any value is valid).</param>
    /// <param name="result">The parsed result if successful.</param>
    /// <returns>True if parsing succeeded; false otherwise.</returns>
    public static bool TryParse(string value, string? constraint, out object? result)
    {
        // Null constraint means string - always succeeds
        if (constraint == null)
        {
            result = value;
            return true;
        }

        return constraint.ToLowerInvariant() switch
        {
            "int" => TryParseInt(value, out result),
            "guid" => TryParseGuid(value, out result),
            "bool" => TryParseBool(value, out result),
            _ => TryParseString(value, out result)
        };
    }

    /// <summary>
    /// Gets the C# type for a constraint.
    /// </summary>
    public static Type GetConstraintType(string? constraint) => constraint?.ToLowerInvariant() switch
    {
        "int" => typeof(int),
        "guid" => typeof(Guid),
        "bool" => typeof(bool),
        _ => typeof(string)
    };

    private static bool TryParseInt(string value, out object? result)
    {
        if (int.TryParse(value, out var intValue))
        {
            result = intValue;
            return true;
        }
        result = null;
        return false;
    }

    private static bool TryParseGuid(string value, out object? result)
    {
        if (Guid.TryParse(value, out var guidValue))
        {
            result = guidValue;
            return true;
        }
        result = null;
        return false;
    }

    private static bool TryParseBool(string value, out object? result)
    {
        if (bool.TryParse(value, out var boolValue))
        {
            result = boolValue;
            return true;
        }
        result = null;
        return false;
    }

    private static bool TryParseString(string value, out object? result)
    {
        result = value;
        return true;
    }
}

using Termina.Routing;

namespace Termina.Tests.Routing;

/// <summary>
/// Tests for route constraint validation and parsing.
/// </summary>
public class RouteConstraintTests
{
    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    [InlineData("-1", -1)]
    [InlineData("2147483647", int.MaxValue)]
    public void TryParse_IntConstraint_ValidValues(string value, int expected)
    {
        var success = RouteConstraints.TryParse(value, "int", out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    [InlineData("9999999999999")]
    public void TryParse_IntConstraint_InvalidValues(string value)
    {
        var success = RouteConstraints.TryParse(value, "int", out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_GuidConstraint_ValidGuid()
    {
        var testGuid = Guid.NewGuid();
        var success = RouteConstraints.TryParse(testGuid.ToString(), "guid", out var result);

        Assert.True(success);
        Assert.Equal(testGuid, result);
    }

    [Fact]
    public void TryParse_GuidConstraint_ValidGuidWithDifferentFormat()
    {
        var testGuid = Guid.NewGuid();
        var success = RouteConstraints.TryParse(testGuid.ToString("N"), "guid", out var result);

        Assert.True(success);
        Assert.Equal(testGuid, result);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    [InlineData("")]
    public void TryParse_GuidConstraint_InvalidValues(string value)
    {
        var success = RouteConstraints.TryParse(value, "guid", out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("TRUE", true)]
    [InlineData("FALSE", false)]
    public void TryParse_BoolConstraint_ValidValues(string value, bool expected)
    {
        var success = RouteConstraints.TryParse(value, "bool", out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("no")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("")]
    public void TryParse_BoolConstraint_InvalidValues(string value)
    {
        var success = RouteConstraints.TryParse(value, "bool", out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("with-dashes")]
    [InlineData("123")]
    [InlineData("")]
    public void TryParse_NoConstraint_AllValuesAsString(string value)
    {
        var success = RouteConstraints.TryParse(value, null, out var result);

        Assert.True(success);
        Assert.Equal(value, result);
    }

    [Fact]
    public void GetConstraintType_Int_ReturnsInt32()
    {
        var type = RouteConstraints.GetConstraintType("int");
        Assert.Equal(typeof(int), type);
    }

    [Fact]
    public void GetConstraintType_Guid_ReturnsGuid()
    {
        var type = RouteConstraints.GetConstraintType("guid");
        Assert.Equal(typeof(Guid), type);
    }

    [Fact]
    public void GetConstraintType_Bool_ReturnsBoolean()
    {
        var type = RouteConstraints.GetConstraintType("bool");
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void GetConstraintType_Null_ReturnsString()
    {
        var type = RouteConstraints.GetConstraintType(null);
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void ValidConstraints_ContainsExpectedTypes()
    {
        Assert.Contains("int", RouteConstraints.ValidConstraints);
        Assert.Contains("guid", RouteConstraints.ValidConstraints);
        Assert.Contains("bool", RouteConstraints.ValidConstraints);
    }

    [Fact]
    public void ValidConstraints_IsCaseInsensitive()
    {
        // The set should use case-insensitive comparison
        Assert.Contains("INT", RouteConstraints.ValidConstraints);
        Assert.Contains("Guid", RouteConstraints.ValidConstraints);
        Assert.Contains("BOOL", RouteConstraints.ValidConstraints);
    }
}

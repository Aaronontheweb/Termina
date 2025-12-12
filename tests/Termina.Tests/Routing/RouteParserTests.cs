using Termina.Routing;

namespace Termina.Tests.Routing;

/// <summary>
/// Tests for route template parsing.
/// </summary>
public class RouteParserTests
{
    [Fact]
    public void Parse_SimpleRoute_ReturnsTemplate()
    {
        var template = RouteParser.Parse("/");

        Assert.Equal("/", template.Template);
        Assert.Empty(template.Segments); // Root path has no segments
        Assert.False(template.HasParameters);
    }

    [Fact]
    public void Parse_LiteralSegments_ParsesCorrectly()
    {
        var template = RouteParser.Parse("/tasks/list");

        Assert.Equal("/tasks/list", template.Template);
        Assert.Equal(2, template.Segments.Count);
        Assert.False(template.Segments[0].IsParameter);
        Assert.Equal("tasks", template.Segments[0].Value);
        Assert.False(template.Segments[1].IsParameter);
        Assert.Equal("list", template.Segments[1].Value);
    }

    [Fact]
    public void Parse_ParameterWithoutConstraint_ParsesCorrectly()
    {
        var template = RouteParser.Parse("/users/{name}");

        Assert.Equal("/users/{name}", template.Template);
        Assert.Equal(2, template.Segments.Count);
        Assert.True(template.Segments[1].IsParameter);
        Assert.Equal("name", template.Segments[1].Value);
        Assert.Null(template.Segments[1].Constraint);
    }

    [Fact]
    public void Parse_ParameterWithIntConstraint_ParsesCorrectly()
    {
        var template = RouteParser.Parse("/tasks/{id:int}");

        Assert.Equal("/tasks/{id:int}", template.Template);
        Assert.Equal(2, template.Segments.Count);
        Assert.True(template.Segments[1].IsParameter);
        Assert.Equal("id", template.Segments[1].Value);
        Assert.Equal("int", template.Segments[1].Constraint);
    }

    [Fact]
    public void Parse_ParameterWithGuidConstraint_ParsesCorrectly()
    {
        var template = RouteParser.Parse("/documents/{id:guid}");

        Assert.Equal("/documents/{id:guid}", template.Template);
        Assert.True(template.Segments[1].IsParameter);
        Assert.Equal("guid", template.Segments[1].Constraint);
    }

    [Fact]
    public void Parse_ParameterWithBoolConstraint_ParsesCorrectly()
    {
        var template = RouteParser.Parse("/items/{active:bool}");

        Assert.Equal("/items/{active:bool}", template.Template);
        Assert.True(template.Segments[1].IsParameter);
        Assert.Equal("bool", template.Segments[1].Constraint);
    }

    [Fact]
    public void Parse_MultipleParameters_ParsesCorrectly()
    {
        var template = RouteParser.Parse("/projects/{projectId:int}/tasks/{taskId:int}");

        Assert.Equal(4, template.Segments.Count);
        Assert.Equal(2, template.ParameterNames.Count);
        Assert.Contains("projectId", template.ParameterNames);
        Assert.Contains("taskId", template.ParameterNames);
    }

    [Fact]
    public void Parse_InvalidConstraint_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RouteParser.Parse("/tasks/{id:invalid}"));

        Assert.Contains("invalid", exception.Message);
    }

    [Fact]
    public void Parse_InvalidParameterSyntax_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RouteParser.Parse("/tasks/{}"));

        Assert.Contains("Invalid", exception.Message);
    }

    [Fact]
    public void Parse_EmptyTemplate_Throws()
    {
        Assert.Throws<ArgumentException>(() => RouteParser.Parse(""));
    }

    [Fact]
    public void Parse_NullTemplate_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RouteParser.Parse(null!));
    }

    [Fact]
    public void Parse_MissingLeadingSlash_AutoCorrected()
    {
        // The parser auto-corrects missing leading slashes
        var template = RouteParser.Parse("tasks/{id}");

        Assert.Equal("/tasks/{id}", template.Template);
        Assert.True(template.HasParameters);
    }

    [Fact]
    public void TryParse_ValidTemplate_ReturnsTrue()
    {
        var success = RouteParser.TryParse("/tasks/{id:int}", out var template);

        Assert.True(success);
        Assert.NotNull(template);
        Assert.True(template!.HasParameters);
    }

    [Fact]
    public void TryParse_InvalidTemplate_ReturnsFalse()
    {
        var success = RouteParser.TryParse("/tasks/{id:invalid}", out var template);

        Assert.False(success);
        Assert.Null(template);
    }
}

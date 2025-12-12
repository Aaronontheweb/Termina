using Termina.Routing;

namespace Termina.Tests.Routing;

/// <summary>
/// Tests for route matching logic.
/// </summary>
public class RouteMatcherTests
{
    [Fact]
    public void TryMatch_ExactLiteralPath_Matches()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks"), "tasks-page");

        var success = matcher.TryMatch("/tasks", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("tasks-page", pageKey);
        Assert.NotNull(parameters);
        Assert.Empty(parameters);
    }

    [Fact]
    public void TryMatch_RootPath_Matches()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/"), "home-page");

        var success = matcher.TryMatch("/", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("home-page", pageKey);
    }

    [Fact]
    public void TryMatch_PathWithIntParameter_Matches()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks/{id:int}"), "task-detail");

        var success = matcher.TryMatch("/tasks/42", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("task-detail", pageKey);
        Assert.NotNull(parameters);
        Assert.Single(parameters);
        Assert.Equal(42, parameters["id"]);
    }

    [Fact]
    public void TryMatch_PathWithGuidParameter_Matches()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/documents/{id:guid}"), "document-page");
        var testGuid = Guid.NewGuid();

        var success = matcher.TryMatch($"/documents/{testGuid}", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("document-page", pageKey);
        Assert.Equal(testGuid, parameters!["id"]);
    }

    [Fact]
    public void TryMatch_PathWithBoolParameter_Matches()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/items/{active:bool}"), "items-page");

        var success = matcher.TryMatch("/items/true", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("items-page", pageKey);
        Assert.True((bool)parameters!["active"]);
    }

    [Fact]
    public void TryMatch_PathWithStringParameter_Matches()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/users/{name}"), "user-page");

        var success = matcher.TryMatch("/users/john", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("user-page", pageKey);
        Assert.Equal("john", parameters!["name"]);
    }

    [Fact]
    public void TryMatch_MultipleParameters_ExtractsAll()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/projects/{projectId:int}/tasks/{taskId:int}"), "task-page");

        var success = matcher.TryMatch("/projects/1/tasks/42", out var pageKey, out var parameters);

        Assert.True(success);
        Assert.Equal("task-page", pageKey);
        Assert.Equal(2, parameters!.Count);
        Assert.Equal(1, parameters["projectId"]);
        Assert.Equal(42, parameters["taskId"]);
    }

    [Fact]
    public void TryMatch_ConstraintMismatch_DoesNotMatch()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks/{id:int}"), "task-detail");

        var success = matcher.TryMatch("/tasks/abc", out var pageKey, out var parameters);

        Assert.False(success);
        Assert.Null(pageKey);
        Assert.Null(parameters);
    }

    [Fact]
    public void TryMatch_WrongSegmentCount_DoesNotMatch()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks/{id:int}"), "task-detail");

        var success = matcher.TryMatch("/tasks/42/extra", out var pageKey, out var parameters);

        Assert.False(success);
    }

    [Fact]
    public void TryMatch_NoMatchingRoute_DoesNotMatch()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks"), "tasks-page");

        var success = matcher.TryMatch("/users", out var pageKey, out var parameters);

        Assert.False(success);
    }

    [Fact]
    public void TryMatch_MultipleRoutes_SelectsCorrectOne()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks"), "tasks-list");
        matcher.AddRoute(RouteParser.Parse("/tasks/{id:int}"), "task-detail");
        matcher.AddRoute(RouteParser.Parse("/users"), "users-list");

        var success1 = matcher.TryMatch("/tasks", out var pageKey1, out _);
        var success2 = matcher.TryMatch("/tasks/42", out var pageKey2, out _);
        var success3 = matcher.TryMatch("/users", out var pageKey3, out _);

        Assert.True(success1);
        Assert.Equal("tasks-list", pageKey1);

        Assert.True(success2);
        Assert.Equal("task-detail", pageKey2);

        Assert.True(success3);
        Assert.Equal("users-list", pageKey3);
    }

    [Fact]
    public void TryMatch_TrailingSlash_DoesNotAffectMatching()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(RouteParser.Parse("/tasks"), "tasks-page");

        // Trailing slash in path should still match
        var success = matcher.TryMatch("/tasks/", out var pageKey, out _);

        Assert.True(success);
        Assert.Equal("tasks-page", pageKey);
    }

    [Fact]
    public void BuildPath_SimpleTemplate_ReturnsPath()
    {
        var path = RouteMatcher.BuildPath("/tasks", null);
        Assert.Equal("/tasks", path);
    }

    [Fact]
    public void BuildPath_WithIntParameter_SubstitutesValue()
    {
        var path = RouteMatcher.BuildPath("/tasks/{id}", new { id = 42 });
        Assert.Equal("/tasks/42", path);
    }

    [Fact]
    public void BuildPath_WithGuidParameter_SubstitutesValue()
    {
        var testGuid = Guid.NewGuid();
        var path = RouteMatcher.BuildPath("/documents/{id:guid}", new { id = testGuid });
        Assert.Equal($"/documents/{testGuid}", path);
    }

    [Fact]
    public void BuildPath_MultipleParameters_SubstitutesAll()
    {
        var path = RouteMatcher.BuildPath(
            "/projects/{projectId}/tasks/{taskId}",
            new { projectId = 1, taskId = 42 });

        Assert.Equal("/projects/1/tasks/42", path);
    }

    [Fact]
    public void BuildPath_MissingParameter_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RouteMatcher.BuildPath("/tasks/{id}", new { wrongName = 42 }));
    }

    [Fact]
    public void BuildPath_NullValues_WithNoParameters_Succeeds()
    {
        var path = RouteMatcher.BuildPath("/tasks", null);
        Assert.Equal("/tasks", path);
    }

    [Fact]
    public void BuildPath_CaseInsensitiveParameterMatching()
    {
        var path = RouteMatcher.BuildPath("/tasks/{Id}", new { id = 42 });
        Assert.Equal("/tasks/42", path);
    }
}

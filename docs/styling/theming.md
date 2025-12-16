# Theming

While Termina doesn't have a built-in theme system, you can create consistent visual themes using helper classes and extension methods.

## Creating a Theme

Define your colors and styles in a static class:

```csharp
public static class AppTheme
{
    // Color palette
    public static Color Primary => Color.Cyan;
    public static Color PrimaryBright => Color.BrightCyan;
    public static Color Secondary => Color.Magenta;
    public static Color Accent => Color.Yellow;
    public static Color Muted => Color.Gray;
    public static Color Error => Color.Red;
    public static Color Success => Color.Green;
    public static Color Warning => Color.Yellow;

    // Panel presets
    public static PanelNode PrimaryPanel() => new PanelNode()
        .WithBorder(BorderStyle.Rounded)
        .WithBorderColor(Primary)
        .WithTitleColor(PrimaryBright);

    public static PanelNode SecondaryPanel() => new PanelNode()
        .WithBorder(BorderStyle.Single)
        .WithBorderColor(Muted);

    public static PanelNode ErrorPanel() => new PanelNode()
        .WithBorder(BorderStyle.Double)
        .WithBorderColor(Error)
        .WithTitleColor(Error);

    // Text presets
    public static TextNode Header(string text) =>
        new TextNode(text).Bold().WithForeground(PrimaryBright);

    public static TextNode Label(string text) =>
        new TextNode(text).WithForeground(Muted);

    public static TextNode ErrorText(string text) =>
        new TextNode(text).WithForeground(Error);

    public static TextNode SuccessText(string text) =>
        new TextNode(text).WithForeground(Success);
}
```

## Using the Theme

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    protected override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                AppTheme.PrimaryPanel()
                    .WithTitle("Dashboard")
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(AppTheme.Header("Welcome"))
                            .WithChild(AppTheme.Label("System Status")))
                    .Height(5))
            .WithChild(
                AppTheme.SecondaryPanel()
                    .WithTitle("Details")
                    .WithContent(contentNode)
                    .Fill());
    }
}
```

## Dark/Light Considerations

Termina relies on terminal colors which are controlled by the user's terminal theme. For best results:

1. **Use semantic names** - `Primary`, `Error` instead of specific colors
2. **Prefer indexed colors** - They adapt to terminal themes
3. **Test with different terminals** - Colors render differently
4. **Avoid RGB for primary UI** - Use indexed colors for compatibility

```csharp
// Good - adapts to terminal theme
public static Color Text => Color.Default;
public static Color Subtle => Color.BrightBlack;

// Avoid - fixed color regardless of theme
public static Color Text => Color.FromRgb(255, 255, 255);
```

## Status-Based Styling

Create helpers for common status patterns:

```csharp
public static class StatusStyles
{
    public static (Color Border, Color Text) ForStatus(Status status) => status switch
    {
        Status.Success => (Color.Green, Color.BrightGreen),
        Status.Warning => (Color.Yellow, Color.BrightYellow),
        Status.Error => (Color.Red, Color.BrightRed),
        Status.Info => (Color.Blue, Color.BrightBlue),
        _ => (Color.Gray, Color.White)
    };

    public static PanelNode StatusPanel(Status status, string title) =>
    {
        var (border, text) = ForStatus(status);
        return new PanelNode()
            .WithTitle(title)
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(border)
            .WithTitleColor(text);
    }
}
```

## Extension Methods

Create extension methods for common styling patterns:

```csharp
public static class StyleExtensions
{
    public static TextNode Highlighted(this TextNode node) =>
        node.WithBackground(Color.Yellow).WithForeground(Color.Black);

    public static TextNode Code(this TextNode node) =>
        node.WithForeground(Color.Green);

    public static TextNode Dimmed(this TextNode node) =>
        node.WithForeground(Color.BrightBlack);

    public static PanelNode Focused(this PanelNode node) =>
        node.WithBorderColor(Color.Cyan);
}

// Usage
new TextNode("search term").Highlighted()
new TextNode("var x = 1;").Code()
```

## Consistent Spacing

Define spacing constants for consistent layouts:

```csharp
public static class Spacing
{
    public const int HeaderHeight = 3;
    public const int FooterHeight = 1;
    public const int PanelPadding = 1;
    public const int SectionSpacing = 1;
}

// Usage
Layouts.Vertical()
    .WithChild(header.Height(Spacing.HeaderHeight))
    .WithChild(new EmptyNode().Height(Spacing.SectionSpacing))
    .WithChild(content.Fill())
    .WithChild(footer.Height(Spacing.FooterHeight))
```

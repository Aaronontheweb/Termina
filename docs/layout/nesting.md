# Nesting Layouts

Complex terminal UIs are built by nesting layouts within layouts. This compositional approach lets you create sophisticated interfaces from simple building blocks.

## Basic Nesting

Layouts can contain other layouts as children:

```csharp
Layouts.Vertical()
    .WithChild(header.Height(1))
    .WithChild(
        // Nested horizontal layout for content area
        Layouts.Horizontal()
            .WithChild(sidebar.Width(25))
            .WithChild(mainContent.WidthFill())
            .Fill())  // This nested layout needs Fill() to expand vertically
    .WithChild(footer.Height(1));
```

**Result:**
```
┌─────────────────────────────────────────┐
│ Header                                   │  ← 1 row
├────────────┬────────────────────────────┤
│            │                            │
│  Sidebar   │      Main Content          │  ← Fill
│            │                            │
├────────────┴────────────────────────────┤
│ Footer                                   │  ← 1 row
└─────────────────────────────────────────┘
```

## Dashboard Grid Pattern

Create a grid by nesting horizontal layouts in a vertical container:

```csharp
Layouts.Vertical()
    .WithChild(
        Layouts.Horizontal()
            .WithChild(topLeft.WidthFill())
            .WithChild(topRight.WidthFill())
            .Fill())  // Row expands to fill available height
    .WithChild(
        Layouts.Horizontal()
            .WithChild(bottomLeft.WidthFill())
            .WithChild(bottomRight.WidthFill())
            .Fill());  // Row expands to fill available height
```

**Result:**
```
┌───────────────────┬───────────────────┐
│                   │                   │
│    Top Left       │    Top Right      │
│                   │                   │
├───────────────────┼───────────────────┤
│                   │                   │
│   Bottom Left     │   Bottom Right    │
│                   │                   │
└───────────────────┴───────────────────┘
```

## Three-Tier Application Layout

A common pattern for full applications:

```csharp
protected override ILayoutNode BuildLayout()
{
    return Layouts.Vertical()
        // Tier 1: Header bar
        .WithChild(BuildHeader().Height(1))

        // Tier 2: Main content area
        .WithChild(
            Layouts.Horizontal()
                .WithChild(BuildNavigation().Width(20))
                .WithChild(BuildContentArea().WidthFill())
                .WithChild(BuildDetailsPanel().Width(30))
                .Fill())  // Content row fills available height

        // Tier 3: Status bar
        .WithChild(BuildStatusBar().Height(1));
}

private ILayoutNode BuildHeader()
{
    return Layouts.Horizontal()
        .WithChild(new TextNode("MyApp").Bold().WidthAuto())
        .WithChild(new EmptyNode().WidthFill())
        .WithChild(new TextNode($"User: {username}").WidthAuto());
}

private ILayoutNode BuildStatusBar()
{
    return Layouts.Horizontal()
        .WithChild(statusText.WidthFill())
        .WithChild(new TextNode("[F1] Help [Esc] Quit").WidthAuto());
}
```

## Panels with Internal Layout

`PanelNode` accepts content that can itself be a layout:

```csharp
new PanelNode()
    .WithTitle("User Details")
    .WithBorder(BorderStyle.Rounded)
    .WithContent(
        Layouts.Vertical()
            .WithChild(new TextNode($"Name: {user.Name}"))
            .WithChild(new TextNode($"Email: {user.Email}"))
            .WithChild(new TextNode($"Role: {user.Role}")))
    .Height(6);
```

## Extracting Reusable Components

For complex UIs, extract nested layouts into methods:

```csharp
public class DashboardPage : ReactivePage<DashboardViewModel>
{
    protected override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(BuildHeader().Height(3))
            .WithChild(BuildMainArea().Fill())  // Main area takes remaining space
            .WithChild(BuildFooter().Height(1));
    }

    private ILayoutNode BuildHeader()
    {
        return new PanelNode()
            .WithTitle("Dashboard")
            .WithBorder(BorderStyle.Double)
            .WithContent(new TextNode("Welcome!"));
    }

    private ILayoutNode BuildMainArea()
    {
        return Layouts.Horizontal()
            .WithChild(BuildMetricsPanel().WidthFill(weight: 2))
            .WithChild(BuildActivityPanel().WidthFill(weight: 1));
    }

    private ILayoutNode BuildMetricsPanel()
    {
        return new PanelNode()
            .WithTitle("Metrics")
            .WithContent(
                ViewModel.MetricsChanged
                    .Select(m => new TextNode(FormatMetrics(m)))
                    .AsLayout());
    }

    private ILayoutNode BuildActivityPanel()
    {
        return new PanelNode()
            .WithTitle("Activity")
            .WithContent(
                ViewModel.ActivityChanged
                    .Select(a => new TextNode(FormatActivity(a)))
                    .AsLayout());
    }

    private ILayoutNode BuildFooter()
    {
        return new TextNode("[Q] Quit [R] Refresh")
            .WithForeground(Color.Gray);
    }
}
```

## Nesting with Reactive Content

When nesting reactive content, be mindful of where the reactive boundary is:

```csharp
// Good: Reactive content inside fixed structure
Layouts.Vertical()
    .WithChild(
        new PanelNode()
            .WithTitle("Status")
            .WithContent(
                ViewModel.StatusChanged
                    .Select(s => new TextNode(s))
                    .AsLayout())
            .Height(3))
    .WithChild(content.Fill());

// Bad: Recreating entire structure reactively (inefficient)
ViewModel.StatusChanged
    .Select(s =>
        Layouts.Vertical()
            .WithChild(new TextNode(s).Height(3))
            .WithChild(content.Fill()))
    .AsLayout();
```

## StackLayout for Overlays

Use `StackLayout` when you need to overlay content:

```csharp
new StackLayout(new[]
{
    // Bottom layer: main content
    mainContent,

    // Top layer: modal dialog (rendered on top)
    ViewModel.ShowModalChanged
        .Select(show => show
            ? BuildModalDialog()
            : (ILayoutNode)new EmptyNode())
        .AsLayout()
});
```

## Performance Considerations

1. **Keep reactive boundaries small** - Only wrap the parts that need to update
2. **Avoid deep nesting** - Flatten where possible for better performance
3. **Cache static layouts** - If a layout doesn't change, consider making it a field
4. **Use EmptyNode for placeholders** - Not `null`, which would break the tree

```csharp
// Efficient: only the text updates
new PanelNode()
    .WithContent(
        ViewModel.CountChanged
            .Select(c => new TextNode($"Count: {c}"))
            .AsLayout());

// Less efficient: entire panel recreated on each update
ViewModel.CountChanged
    .Select(c => new PanelNode().WithContent(new TextNode($"Count: {c}")))
    .AsLayout();
```

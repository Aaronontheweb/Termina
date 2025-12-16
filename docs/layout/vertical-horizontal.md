# Vertical & Horizontal Layouts

The two primary container types in Termina are `VerticalLayout` and `HorizontalLayout`. These containers arrange their children in a single direction and handle space distribution automatically.

## VerticalLayout

Arranges children from top to bottom. Each child occupies the full width of the container.

```csharp
Layouts.Vertical()
    .WithChild(header)
    .WithChild(content)
    .WithChild(footer);
```

Or using the constructor:

```csharp
new VerticalLayout(new ILayoutNode[]
{
    header,
    content,
    footer
});
```

### Height Distribution

Children specify their desired height using `HeightConstraint`:

```csharp
Layouts.Vertical()
    .WithChild(new TextNode("Fixed").Height(3))       // 3 rows
    .WithChild(new TextNode("Fill").Fill())           // Remaining space
    .WithChild(new TextNode("Also Fixed").Height(1)); // 1 row
```

### Width Behavior

In a vertical layout, each child is given the full container width by default. Children can use their `WidthConstraint` to request less:

```csharp
Layouts.Vertical()
    .WithChild(new TextNode("Full width"))           // Uses container width
    .WithChild(new TextNode("Half").Width(40));      // Only 40 columns
```

### Spacing

Add spacing between children:

```csharp
Layouts.Vertical(spacing: 1)  // 1 row between each child
    .WithChild(panel1)
    .WithChild(panel2)
    .WithChild(panel3);
```

## HorizontalLayout

Arranges children from left to right. Each child occupies the full height of the container.

```csharp
Layouts.Horizontal()
    .WithChild(sidebar)
    .WithChild(mainContent);
```

### Width Distribution

Children specify their desired width using `WidthConstraint`:

```csharp
Layouts.Horizontal()
    .WithChild(new PanelNode().Width(25))        // Fixed 25 columns
    .WithChild(new PanelNode().WidthFill())      // Remaining space
    .WithChild(new PanelNode().Width(20));       // Fixed 20 columns
```

### Height Behavior

In a horizontal layout, each child is given the full container height by default:

```csharp
Layouts.Horizontal()
    .WithChild(leftPanel)     // Full height
    .WithChild(rightPanel);   // Full height
```

### Spacing

Add spacing between children:

```csharp
Layouts.Horizontal(spacing: 2)  // 2 columns between each child
    .WithChild(col1)
    .WithChild(col2)
    .WithChild(col3);
```

## Common Patterns

### App Shell (Header/Content/Footer)

```csharp
Layouts.Vertical()
    .WithChild(
        new TextNode("My App")
            .Bold()
            .WithForeground(Color.Cyan)
            .Height(1))
    .WithChild(
        mainContent.Fill())
    .WithChild(
        new TextNode("[Esc] Quit")
            .WithForeground(Color.Gray)
            .Height(1));
```

### Two-Column Layout

```csharp
Layouts.Horizontal()
    .WithChild(
        new PanelNode()
            .WithTitle("Sidebar")
            .WithContent(menuItems)
            .Width(30))
    .WithChild(
        new PanelNode()
            .WithTitle("Content")
            .WithContent(mainView)
            .WidthFill());
```

### Three-Column Dashboard

```csharp
Layouts.Horizontal()
    .WithChild(leftPanel.WidthFill(weight: 1))
    .WithChild(centerPanel.WidthFill(weight: 2))
    .WithChild(rightPanel.WidthFill(weight: 1));
```

### Status Bar with Sections

```csharp
Layouts.Horizontal()
    .WithChild(
        new TextNode($"Status: {status}")
            .WidthFill())
    .WithChild(
        new TextNode($"Items: {count}")
            .WidthAuto())
    .WithChild(
        new TextNode("  |  ")
            .WidthAuto())
    .WithChild(
        new TextNode("[Esc] Quit")
            .WidthAuto())
    .Height(1);
```

### Form Layout

```csharp
Layouts.Vertical()
    .WithChild(
        Layouts.Horizontal()
            .WithChild(new TextNode("Username:").Width(12))
            .WithChild(usernameInput.WidthFill())
            .Height(1))
    .WithChild(
        Layouts.Horizontal()
            .WithChild(new TextNode("Password:").Width(12))
            .WithChild(passwordInput.WidthFill())
            .Height(1))
    .WithChild(new EmptyNode().Height(1))
    .WithChild(
        new TextNode("[Enter] Submit")
            .Height(1));
```

## The Layouts Helper Class

The `Layouts` static class provides factory methods for creating layouts:

```csharp
// Create vertical layout
Layouts.Vertical()
Layouts.Vertical(spacing: 1)
Layouts.Vertical(children)
Layouts.Vertical(spacing: 1, children)

// Create horizontal layout
Layouts.Horizontal()
Layouts.Horizontal(spacing: 1)
Layouts.Horizontal(children)
Layouts.Horizontal(spacing: 1, children)
```

## Applying Constraints to Layouts

Layouts themselves are nodes and can have constraints applied:

```csharp
// A horizontal layout that fills height but has fixed width
Layouts.Horizontal()
    .WithChild(...)
    .Width(60)
    .Fill();

// A vertical layout with maximum height
Layouts.Vertical()
    .WithChild(...)
    .Height(SizeConstraint.Auto(max: 20));
```

## When to Use Each

| Use Case | Layout Type |
|----------|-------------|
| Page structure (header/content/footer) | Vertical |
| Sidebar + main content | Horizontal |
| Dashboard columns | Horizontal |
| Form fields | Vertical with nested Horizontal |
| Status bar sections | Horizontal |
| Stacked panels | Vertical |
| Tab bar | Horizontal |

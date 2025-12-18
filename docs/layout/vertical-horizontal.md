# Vertical & Horizontal Layouts

The two primary container types in Termina are `VerticalLayout` and `HorizontalLayout`. These containers arrange their children in a single direction and handle space distribution automatically.

## Default Sizing

Both layout types default to `Auto()` sizing - they size to fit their content. This is the right default for nested layouts, which should not compete with siblings for space.

```csharp
Layouts.Vertical()
    .WithChild(header)
    .WithChild(content)
    .WithChild(footer);
```

::: info Root layouts always fill the terminal
The root layout returned from `BuildLayout()` always receives the full terminal bounds, regardless of its constraints. You don't need to add `.Fill()` to the root layout.
:::

::: tip When to use Fill()
Use `.Fill()` when you want a **child** to expand beyond its content size within a container:
- On **content panels** that should take remaining space after fixed elements
- On **spacer elements** to push siblings apart

See [Size Constraints](/layout/size-constraints#when-to-use-fill) for detailed examples.
:::

## VerticalLayout

Arranges children from top to bottom.

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

## Need Z-Axis Layering?

`VerticalLayout` and `HorizontalLayout` arrange children along a single axis. For **overlapping content** where children render on top of each other (like modals, tooltips, or floating panels), use [StackLayout](/components/stack-layout).

```csharp
// Children render in order - later children appear on top
new StackLayout()
    .WithChild(backgroundContent)
    .WithChild(floatingPanel);
```

See [StackLayout](/components/stack-layout) for details on z-axis composition.

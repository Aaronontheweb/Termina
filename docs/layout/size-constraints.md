# Size Constraints

Size constraints control how layout nodes claim space within their parent container. Every node has two constraint properties:

- `HeightConstraint` - Vertical sizing
- `WidthConstraint` - Horizontal sizing

## The Four Constraint Types

### Fixed

A fixed constraint requests exactly the specified number of rows (height) or columns (width).

```csharp
// Fixed height of 3 rows
new PanelNode().Height(3);
new PanelNode().Height(SizeConstraint.Fixed(3));

// Fixed width of 40 columns
new PanelNode().Width(40);
new PanelNode().Width(SizeConstraint.Fixed(40));
```

Use fixed constraints for:
- Headers and footers
- Status bars
- Panels with known content size
- Spacers between elements

::: tip
If the available space is less than the fixed value, the node will be clamped to fit.
:::

### Fill

A fill constraint expands to consume remaining space after fixed and auto nodes are measured.

```csharp
// Fill all remaining height
new PanelNode().Fill();
new PanelNode().Height(SizeConstraint.Fill());

// Fill all remaining width
new TextNode("content").WidthFill();
new TextNode("content").Width(SizeConstraint.Fill());
```

#### Weighted Fill

When multiple nodes use fill constraints, you can assign **weights** to distribute space proportionally:

```csharp
Layouts.Horizontal()
    .WithChild(sidebar.WidthFill(weight: 1))    // Gets 1/4 of space
    .WithChild(content.WidthFill(weight: 2))    // Gets 2/4 of space
    .WithChild(details.WidthFill(weight: 1));   // Gets 1/4 of space
```

**How weights work:**

Given 100 columns of remaining space and weights `[1, 2, 1]` (total = 4):
- Sidebar: 100 × (1/4) = **25 columns**
- Content: 100 × (2/4) = **50 columns**
- Details: 100 × (1/4) = **25 columns**

If all fills have weight 1 (the default), space is divided equally:

```csharp
// Three equal columns
Layouts.Horizontal()
    .WithChild(left.WidthFill())     // 1/3
    .WithChild(middle.WidthFill())   // 1/3
    .WithChild(right.WidthFill());   // 1/3
```

### Auto

An auto constraint sizes the node to fit its content, optionally with min/max bounds.

```csharp
// Size to content
new TextNode("Hello").HeightAuto();
new TextNode("Hello").Height(SizeConstraint.Auto());

// Size to content, but at least 10 columns
new TextNode("Hi").Width(SizeConstraint.Auto(min: 10));

// Size to content, but at most 50 columns
new TextNode(longText).Width(SizeConstraint.Auto(max: 50));

// Bounded auto
new TextNode(text).Width(SizeConstraint.Auto(min: 10, max: 50));
```

Use auto constraints for:
- Text that should wrap or truncate naturally
- Labels with varying content
- Nodes with unknown but bounded size

### Percent

A percent constraint requests a percentage of the available space (0-100).

```csharp
// 50% of available height
new PanelNode().Height(SizeConstraint.Percent(50));

// 75% of available width
new PanelNode().Width(SizeConstraint.Percent(75));
```

::: warning
Percent constraints are calculated based on available space at measurement time, not the final container size. This can lead to unexpected results in nested layouts.
:::

## The Layout Algorithm

Container layouts (Vertical/Horizontal) use a two-pass algorithm:

**Pass 1: Measure Fixed and Auto**
```
Available: 24 rows
Children: [Fixed(3), Fill(), Fixed(1)]

Fixed 3 → 3 rows claimed
Fixed 1 → 1 row claimed
Total claimed: 4 rows
```

**Pass 2: Distribute Remaining to Fill**
```
Remaining: 24 - 4 = 20 rows
Fill weight 1 → 20 rows
```

**Final layout:**
```
Row 0-2:   Header (3 rows)
Row 3-22:  Content (20 rows)
Row 23:    Footer (1 row)
```

## Common Patterns

### Header + Content + Footer

```csharp
Layouts.Vertical()
    .WithChild(new TextNode("Header").Height(1))
    .WithChild(content.Fill())
    .WithChild(new TextNode("Footer").Height(1));
```

### Sidebar + Main Content

```csharp
Layouts.Horizontal()
    .WithChild(sidebar.Width(25))      // Fixed 25 columns
    .WithChild(mainContent.WidthFill()); // Fill remaining
```

### Dashboard Grid

```csharp
Layouts.Vertical()
    .WithChild(
        Layouts.Horizontal()
            .WithChild(panel1.WidthFill())
            .WithChild(panel2.WidthFill())
            .Fill())
    .WithChild(
        Layouts.Horizontal()
            .WithChild(panel3.WidthFill())
            .WithChild(panel4.WidthFill())
            .Fill());
```

### Status Bar with Spacer

```csharp
Layouts.Horizontal()
    .WithChild(new TextNode("Status: Ready").WidthAuto())
    .WithChild(new EmptyNode().WidthFill())  // Spacer
    .WithChild(new TextNode("[Esc] Quit").WidthAuto())
    .Height(1);
```

## Fluent API Reference

| Method | Constraint | Notes |
|--------|------------|-------|
| `.Height(n)` | Fixed | Shorthand for Fixed(n) |
| `.Width(n)` | Fixed | Shorthand for Fixed(n) |
| `.Fill()` | Fill (height) | Default weight = 1 |
| `.Fill(weight)` | Fill (height) | Weighted fill |
| `.WidthFill()` | Fill (width) | Default weight = 1 |
| `.WidthFill(weight)` | Fill (width) | Weighted fill |
| `.HeightAuto()` | Auto (height) | Size to content |
| `.WidthAuto()` | Auto (width) | Size to content |
| `.Height(constraint)` | Any | Pass SizeConstraint directly |
| `.Width(constraint)` | Any | Pass SizeConstraint directly |

# Responsive Design

Termina automatically handles terminal resize events, re-measuring and re-rendering your layout when the window size changes. This page covers how to build UIs that adapt gracefully to different terminal sizes.

## Automatic Resize Handling

Termina polls for terminal size changes and triggers a full re-layout when detected. Your `BuildLayout()` method doesn't need any special code - the layout system handles it automatically.

```csharp
protected override ILayoutNode BuildLayout()
{
    // This layout automatically adapts to any terminal size
    return Layouts.Vertical()
        .WithChild(header.Height(1))
        .WithChild(content.Fill())  // Expands/contracts with terminal
        .WithChild(footer.Height(1));
}
```

## Using Fill for Responsive Content

The `Fill` constraint is key to responsive layouts. Fill nodes automatically adjust when the terminal is resized:

```csharp
Layouts.Horizontal()
    .WithChild(sidebar.Width(25))       // Fixed: always 25 columns
    .WithChild(mainContent.WidthFill()); // Responsive: uses remaining space

// Terminal 80 cols: sidebar=25, content=55
// Terminal 120 cols: sidebar=25, content=95
// Terminal 60 cols: sidebar=25, content=35
```

## Weighted Fill for Proportional Layouts

Use weighted fills to maintain proportions as the terminal resizes:

```csharp
Layouts.Horizontal()
    .WithChild(left.WidthFill(weight: 1))   // 25%
    .WithChild(center.WidthFill(weight: 2)) // 50%
    .WithChild(right.WidthFill(weight: 1)); // 25%

// Terminal 80 cols: left=20, center=40, right=20
// Terminal 120 cols: left=30, center=60, right=30
```

## Minimum and Maximum Sizes

Use `Auto` constraints with min/max to set boundaries:

```csharp
// Sidebar: at least 20 columns, at most 40
new PanelNode()
    .WithTitle("Sidebar")
    .Width(SizeConstraint.Auto(min: 20, max: 40));

// Content area: at least 30 rows
Layouts.Vertical()
    .WithChild(content)
    .Height(SizeConstraint.Auto(min: 30));
```

## Handling Small Terminals

When the terminal is very small, fixed-size elements may not fit. Consider:

### Priority-Based Layout

Put the most important content in Fill containers:

```csharp
Layouts.Vertical()
    .WithChild(header.Height(1))          // May get clipped in tiny terminals
    .WithChild(content.Fill())            // Gets whatever space remains
    .WithChild(footer.Height(1));         // May get clipped
```

### Conditional Content

Use `ConditionalNode` to hide elements in small terminals:

```csharp
// Note: This requires tracking terminal size in your ViewModel
ViewModel.TerminalSizeChanged
    .Select(size => size.Height > 20
        ? BuildFullLayout()
        : BuildCompactLayout())
    .AsLayout();
```

## Terminal Size in ViewModels

Access terminal size through the resize event stream:

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    [Reactive] private Size _terminalSize;

    public override void OnActivated()
    {
        Input.OfType<TerminalResized>()
            .Subscribe(e => TerminalSize = new Size(e.Width, e.Height))
            .DisposeWith(Subscriptions);
    }
}
```

## Best Practices

### 1. Prefer Fill Over Fixed

```csharp
// Good: Adapts to terminal size
new PanelNode().WidthFill();

// Less flexible: Always 40 columns
new PanelNode().Width(40);
```

### 2. Use Fixed for Boundaries, Fill for Content

```csharp
Layouts.Vertical()
    .WithChild(header.Height(1))    // Fixed boundaries
    .WithChild(content.Fill())      // Flexible content
    .WithChild(footer.Height(1));   // Fixed boundaries
```

### 3. Test at Different Sizes

Run your app and resize the terminal to verify:
- Content doesn't overflow
- Important information remains visible
- Layout doesn't break at extreme sizes

### 4. Consider Minimum Viable Size

Design for a minimum reasonable terminal size (e.g., 80x24) and ensure your app is usable at that size.

## Resize Polling

Termina polls for terminal size changes at a configurable interval (default: 100ms). This ensures resize events are detected even when the terminal doesn't send SIGWINCH signals.

The polling happens automatically in the background - you don't need to configure anything for basic resize handling.

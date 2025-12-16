# EmptyNode

A placeholder node that renders nothing and takes no space.

## Basic Usage

```csharp
new EmptyNode()
```

## Use Cases

### Placeholder for Conditional Content

```csharp
// Return EmptyNode when nothing should be shown
ViewModel.ErrorChanged
    .Select(error => string.IsNullOrEmpty(error)
        ? (ILayoutNode)new EmptyNode()
        : new TextNode(error).WithForeground(Color.Red))
    .AsLayout()
```

### Spacer Between Elements

```csharp
Layouts.Vertical()
    .WithChild(header.Height(1))
    .WithChild(new EmptyNode().Height(1))  // 1-row spacer
    .WithChild(content.Fill())
```

### Flexible Spacer

```csharp
// Push content to edges
Layouts.Horizontal()
    .WithChild(leftContent.WidthAuto())
    .WithChild(new EmptyNode().WidthFill())  // Takes remaining space
    .WithChild(rightContent.WidthAuto())
```

### Default for ConditionalNode

```csharp
// EmptyNode is the default else content
new ConditionalNode(
    condition,
    thenNode: visibleContent)  // else is EmptyNode
```

## Important Notes

::: warning
Never use `null` as a layout node. Always use `EmptyNode` as a placeholder.
:::

```csharp
// Bad - null breaks the layout tree
condition ? content : null

// Good - EmptyNode is a valid placeholder
condition ? content : new EmptyNode()
```

## API Reference

### Constructor

```csharp
public EmptyNode()
```

### Behavior

- `Measure()` returns `Size(0, 0)` by default
- `Render()` does nothing
- Can be given size constraints to act as a spacer

### Spacer Pattern

```csharp
// Fixed spacer
new EmptyNode().Height(2)          // 2-row vertical spacer
new EmptyNode().Width(4)           // 4-column horizontal spacer

// Flexible spacer
new EmptyNode().Fill()             // Fill remaining height
new EmptyNode().WidthFill()        // Fill remaining width
```

## Source Code

::: details View EmptyNode implementation
<<< @/../src/Termina/Layout/EmptyNode.cs{csharp}
:::

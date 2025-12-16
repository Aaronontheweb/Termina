# ConditionalNode

Shows or hides content based on an observable boolean condition.

## Basic Usage

```csharp
// Show content when condition is true
new ConditionalNode(
    ViewModel.IsVisibleChanged,
    new TextNode("I'm visible!"))

// Using When helper
When.True(
    ViewModel.IsVisibleChanged,
    new TextNode("I'm visible!"))
```

## With Else Content

```csharp
// Show different content based on condition
new ConditionalNode(
    ViewModel.IsLoadingChanged,
    thenNode: new SpinnerNode().WithLabel("Loading..."),
    elseNode: new TextNode("Ready"))

// Using When helper
When.TrueElse(
    ViewModel.IsLoadingChanged,
    thenContent: new SpinnerNode().WithLabel("Loading..."),
    elseContent: new TextNode("Ready"))
```

## Inverted Condition

```csharp
// Show content when condition is false
When.False(
    ViewModel.HasDataChanged,
    new TextNode("No data available"))
```

## Common Patterns

### Loading State

```csharp
new ConditionalNode(
    ViewModel.IsLoadingChanged,
    thenNode: new SpinnerNode().WithLabel("Loading..."),
    elseNode: contentPanel)
```

### Error Display

```csharp
When.True(
    ViewModel.HasErrorChanged,
    new PanelNode()
        .WithTitle("Error")
        .WithBorderColor(Color.Red)
        .WithContent(
            ViewModel.ErrorMessageChanged
                .Select(msg => new TextNode(msg).WithForeground(Color.Red))
                .AsLayout()))
```

### Feature Toggle

```csharp
When.True(
    ViewModel.IsAdvancedModeChanged,
    advancedOptionsPanel)
```

### Empty State

```csharp
When.TrueElse(
    ViewModel.ItemsChanged.Select(items => items.Count > 0),
    thenContent: itemListPanel,
    elseContent: new TextNode("No items yet")
        .WithForeground(Color.Gray))
```

## Comparison with ReactiveLayoutNode

Both can be used for conditional rendering:

```csharp
// Using ConditionalNode - cleaner for simple show/hide
When.True(condition, content)

// Using ReactiveLayoutNode - more flexible
condition
    .Select(show => show ? content : new EmptyNode())
    .AsLayout()
```

Use `ConditionalNode` when:
- You have a simple true/false condition
- The content nodes are known at construction time
- You want cleaner, more readable code

Use `ReactiveLayoutNode` when:
- You need to transform values into different layouts
- You're building dynamic content based on observable values

## API Reference

### Constructors

```csharp
public ConditionalNode(
    IObservable<bool> condition,
    ILayoutNode thenNode,
    ILayoutNode? elseNode = null)
```

### When Helper Methods

```csharp
// Show when true
When.True(IObservable<bool> condition, ILayoutNode content)

// Show when true, else show other content
When.TrueElse(
    IObservable<bool> condition,
    ILayoutNode thenContent,
    ILayoutNode elseContent)

// Show when false
When.False(IObservable<bool> condition, ILayoutNode content)
```

## Source Code

::: details View ConditionalNode implementation
<<< @/../src/Termina/Layout/ConditionalNode.cs{csharp}
:::

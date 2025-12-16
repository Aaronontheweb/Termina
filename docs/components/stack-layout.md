# StackLayout

A container that overlays children in the same space (z-stack). The last child renders on top.

## Basic Usage

```csharp
new StackLayout(new ILayoutNode[]
{
    backgroundContent,   // Bottom layer
    overlayContent       // Top layer (renders on top)
})
```

## Use Cases

### Modal Dialogs

```csharp
new StackLayout(new ILayoutNode[]
{
    // Main content (always visible)
    mainPageContent,

    // Modal overlay (conditionally visible)
    ViewModel.ShowModalChanged
        .Select(show => show
            ? BuildModalDialog()
            : (ILayoutNode)new EmptyNode())
        .AsLayout()
})
```

### Loading Overlay

```csharp
new StackLayout(new ILayoutNode[]
{
    // Main content
    dataView,

    // Loading spinner overlay
    ViewModel.IsLoadingChanged
        .Select(loading => loading
            ? Layouts.Vertical()
                .WithChild(new EmptyNode().Fill())
                .WithChild(new SpinnerNode().WithLabel("Loading...").HeightAuto())
                .WithChild(new EmptyNode().Fill())
            : (ILayoutNode)new EmptyNode())
        .AsLayout()
})
```

### Toast Notifications

```csharp
new StackLayout(new ILayoutNode[]
{
    mainContent,

    // Toast in bottom-right corner
    ViewModel.ToastMessageChanged
        .Select(msg => string.IsNullOrEmpty(msg)
            ? (ILayoutNode)new EmptyNode()
            : Layouts.Vertical()
                .WithChild(new EmptyNode().Fill())  // Push to bottom
                .WithChild(
                    Layouts.Horizontal()
                        .WithChild(new EmptyNode().WidthFill())  // Push to right
                        .WithChild(
                            new PanelNode()
                                .WithContent(new TextNode(msg))
                                .Width(30)
                                .Height(3))))
        .AsLayout()
})
```

## Render Order

Children are rendered in array order - first child is bottom, last child is top:

```csharp
new StackLayout(new ILayoutNode[]
{
    layer1,  // Rendered first (bottom)
    layer2,  // Rendered second
    layer3   // Rendered last (top, visible if overlapping)
})
```

## Transparency

Stack layers can have "transparent" areas by simply not writing to them. Child nodes only overwrite the areas they render to.

## API Reference

### Constructor

```csharp
public StackLayout(IEnumerable<ILayoutNode> children)
```

### Behavior

- All children receive the same bounds
- Children render in order (first = bottom, last = top)
- Last child's content overwrites previous layers where they overlap
- Empty areas are "transparent" - underlying layers show through

## Source Code

::: details View StackLayout implementation
<<< @/../src/Termina/Layout/StackLayout.cs{csharp}
:::

# PanelNode

A bordered container with optional title and content.

## Basic Usage

```csharp
new PanelNode()
    .WithTitle("My Panel")
    .WithContent(new TextNode("Panel content"))
```

## Border Styles

Termina supports four border styles:

```csharp
// Single line border (default)
new PanelNode().WithBorder(BorderStyle.Single)
// ┌─────────┐
// │ Content │
// └─────────┘

// Double line border
new PanelNode().WithBorder(BorderStyle.Double)
// ╔═════════╗
// ║ Content ║
// ╚═════════╝

// Rounded corners
new PanelNode().WithBorder(BorderStyle.Rounded)
// ╭─────────╮
// │ Content │
// ╰─────────╯

// ASCII fallback
new PanelNode().WithBorder(BorderStyle.Ascii)
// +---------+
// | Content |
// +---------+
```

## Styling

```csharp
new PanelNode()
    .WithTitle("Styled Panel")
    .WithBorder(BorderStyle.Rounded)
    .WithBorderColor(Color.Blue)
    .WithTitleColor(Color.Cyan)
    .WithPadding(1)
    .WithContent(content)
```

## Content Types

Panel content can be any layout node or plain text:

```csharp
// Text string (auto-wrapped in TextNode)
new PanelNode().WithContent("Simple text")

// Layout node
new PanelNode().WithContent(new TextNode("Styled").Bold())

// Nested layouts
new PanelNode().WithContent(
    Layouts.Vertical()
        .WithChild(new TextNode("Line 1"))
        .WithChild(new TextNode("Line 2")))
```

## Reactive Content

Bind panel content to observables:

```csharp
new PanelNode()
    .WithTitle("Counter")
    .WithContent(
        ViewModel.CountChanged
            .Select(c => new TextNode($"Value: {c}"))
            .AsLayout())
```

## Size Constraints

PanelNode defaults to `HeightConstraint = Auto` and `WidthConstraint = Fill`:

```csharp
// Fixed height panel
new PanelNode()
    .WithContent(content)
    .Height(5)

// Fill available space
new PanelNode()
    .WithContent(content)
    .Fill()
```

## API Reference

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string?` | `null` | Title in top border |
| `Border` | `BorderStyle` | `Single` | Border style |
| `BorderColor` | `Color?` | `null` | Border color |
| `TitleColor` | `Color?` | `null` | Title color |
| `Padding` | `int` | `0` | Inner padding |

### Fluent Methods

| Method | Description |
|--------|-------------|
| `.WithTitle(string)` | Set the panel title |
| `.WithBorder(BorderStyle)` | Set border style |
| `.WithBorderColor(Color)` | Set border color |
| `.WithTitleColor(Color)` | Set title color |
| `.WithContent(ILayoutNode)` | Set content node |
| `.WithContent(string)` | Set text content |
| `.WithPadding(int)` | Set inner padding |

## Source Code

::: details View PanelNode implementation
<<< @/../src/Termina/Layout/PanelNode.cs{csharp}
:::

# Styling Guide

Termina provides a rich set of styling options for text and borders. All styling is done through fluent APIs on layout nodes, with no CSS or external stylesheets.

## Overview

Styling in Termina includes:

- **Colors** - Foreground and background colors for text and borders
- **Text Formatting** - Bold, italic, underline
- **Border Styles** - Single, double, rounded, and ASCII borders

## Quick Examples

### Styled Text

```csharp
new TextNode("Important message")
    .WithForeground(Color.Red)
    .Bold()
```

### Styled Panel

```csharp
new PanelNode()
    .WithTitle("Dashboard")
    .WithBorder(BorderStyle.Rounded)
    .WithBorderColor(Color.Cyan)
    .WithTitleColor(Color.BrightCyan)
```

### Colored Spinners

```csharp
new SpinnerNode()
    .WithLabel("Processing...")
    .WithSpinnerColor(Color.Yellow)
    .WithLabelColor(Color.Gray)
```

## Color System

Termina supports three color modes:

| Mode | Description | Example |
|------|-------------|---------|
| Default | Terminal's default color | `Color.Default` |
| Indexed | 256-color palette | `Color.Red`, `Color.FromIndex(42)` |
| RGB | True color (24-bit) | `Color.FromRgb(255, 128, 0)` |

See [Colors](/styling/colors) for details.

## Text Formatting

Apply text styles using fluent methods:

```csharp
new TextNode("Formatted")
    .Bold()
    .Italic()
    .Underline()
```

See [Text Formatting](/styling/text-formatting) for details.

## Border Styles

Four border styles are available:

```
Single:   ┌───┐     Double:   ╔═══╗
          │   │               ║   ║
          └───┘               ╚═══╝

Rounded:  ╭───╮     ASCII:    +---+
          │   │               |   |
          ╰───╯               +---+
```

See [Borders](/styling/borders) for details.

## Sections

- [Colors](/styling/colors) - Color modes and named colors
- [Text Formatting](/styling/text-formatting) - Bold, italic, underline
- [Borders](/styling/borders) - Border styles and colors
- [Theming](/styling/theming) - Creating consistent visual themes

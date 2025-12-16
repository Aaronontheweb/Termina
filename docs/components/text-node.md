# TextNode

Renders styled text with optional word wrapping, colors, and formatting.

## Basic Usage

```csharp
new TextNode("Hello, World!")
```

## Styling

```csharp
new TextNode("Styled text")
    .WithForeground(Color.Cyan)
    .WithBackground(Color.Blue)
    .Bold()
    .Italic()
    .Underline();
```

## Word Wrapping

Word wrapping is **enabled by default**. Text will wrap at word boundaries when it exceeds the available width.

```csharp
// Word wrap enabled (default)
new TextNode("This long text will wrap to multiple lines when needed")

// Disable word wrap (truncate instead)
new TextNode("This text will be truncated if too long")
    .NoWrap();
```

## Multi-line Text

TextNode supports newlines in content:

```csharp
new TextNode("Line 1\nLine 2\nLine 3")
```

## Size Constraints

TextNode defaults to `HeightConstraint = Auto` and `WidthConstraint = Fill`:

```csharp
// Default: fills width, auto height based on content
new TextNode("Content")

// Fixed height (may clip content)
new TextNode("Content").Height(3)

// Fixed width (triggers wrapping or truncation)
new TextNode("Content").Width(40)
```

## API Reference

### Constructor

```csharp
public TextNode(string content)
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Content` | `string` | - | The text content |
| `Foreground` | `Color?` | `null` | Foreground color |
| `Background` | `Color?` | `null` | Background color |
| `IsBold` | `bool` | `false` | Whether text is bold |
| `IsItalic` | `bool` | `false` | Whether text is italic |
| `IsUnderline` | `bool` | `false` | Whether text is underlined |
| `WordWrap` | `bool` | `true` | Whether to wrap text |

### Fluent Methods

| Method | Description |
|--------|-------------|
| `.WithForeground(Color)` | Set foreground color |
| `.WithBackground(Color)` | Set background color |
| `.Bold()` | Make text bold |
| `.Italic()` | Make text italic |
| `.Underline()` | Make text underlined |
| `.NoWrap()` | Disable word wrapping |

## Source Code

::: details View TextNode implementation
<<< @/../src/Termina/Layout/TextNode.cs{csharp}
:::

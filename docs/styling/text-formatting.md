# Text Formatting

Termina supports common text formatting options for TextNode and StreamingTextNode.

## Bold Text

```csharp
new TextNode("Bold text").Bold()
```

## Italic Text

```csharp
new TextNode("Italic text").Italic()
```

## Underlined Text

```csharp
new TextNode("Underlined text").Underline()
```

## Combining Styles

Styles can be chained together:

```csharp
new TextNode("Bold and italic")
    .Bold()
    .Italic()

new TextNode("All styles")
    .Bold()
    .Italic()
    .Underline()
    .WithForeground(Color.Cyan)
```

## Word Wrapping

Control how text handles long lines:

```csharp
// Word wrap enabled (default)
new TextNode("Long text that will wrap at word boundaries")

// Word wrap disabled (truncate)
new TextNode("Text that won't wrap").NoWrap()
```

::: tip When to use NoWrap
Use `.NoWrap()` for:
- Status bar items
- Single-line displays
- Content that should truncate rather than overflow
:::

## Styled Labels Pattern

For consistent styled text throughout your app:

```csharp
// In a helper class
public static class Styles
{
    public static TextNode Header(string text) =>
        new TextNode(text)
            .Bold()
            .WithForeground(Color.BrightCyan);

    public static TextNode Error(string text) =>
        new TextNode(text)
            .WithForeground(Color.Red);

    public static TextNode Success(string text) =>
        new TextNode(text)
            .WithForeground(Color.Green);

    public static TextNode Muted(string text) =>
        new TextNode(text)
            .WithForeground(Color.Gray);
}

// Usage
Layouts.Vertical()
    .WithChild(Styles.Header("Welcome"))
    .WithChild(Styles.Muted("Press any key..."))
```

## Terminal Compatibility

::: warning
Not all terminals support all text styles:

| Style | Compatibility |
|-------|--------------|
| Bold | Wide support |
| Italic | Most modern terminals |
| Underline | Wide support |

Some terminals render bold as bright colors instead of heavier font weight.
:::

## Properties Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsBold` | `bool` | `false` | Bold text |
| `IsItalic` | `bool` | `false` | Italic text |
| `IsUnderline` | `bool` | `false` | Underlined text |
| `WordWrap` | `bool` | `true` | Enable word wrapping |

## StreamingTextNode Inline Styling

For streaming content with inline styles, use the styled append methods:

```csharp
var stream = StreamingTextNode.Create();

// Inline colors and decorations
stream.Append("Error: ", foreground: Color.Red, decoration: TextDecoration.Bold);
stream.AppendLine("Something went wrong");

// Combine multiple decorations
stream.Append("Important",
    foreground: Color.Yellow,
    decoration: TextDecoration.Bold | TextDecoration.Underline);
```

See [StreamingTextNode](/components/streaming-text-node#inline-styled-text) for full documentation on inline styled text.

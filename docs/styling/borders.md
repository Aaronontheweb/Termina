# Borders

PanelNode supports four border styles with customizable colors.

## Border Styles

### Single (Default)

Standard single-line border:

```csharp
new PanelNode()
    .WithBorder(BorderStyle.Single)
    .WithContent("Content")
```

```
┌──────────┐
│ Content  │
└──────────┘
```

### Double

Double-line border for emphasis:

```csharp
new PanelNode()
    .WithBorder(BorderStyle.Double)
    .WithContent("Content")
```

```
╔══════════╗
║ Content  ║
╚══════════╝
```

### Rounded

Single-line border with rounded corners:

```csharp
new PanelNode()
    .WithBorder(BorderStyle.Rounded)
    .WithContent("Content")
```

```
╭──────────╮
│ Content  │
╰──────────╯
```

### ASCII

Fallback for limited terminals:

```csharp
new PanelNode()
    .WithBorder(BorderStyle.Ascii)
    .WithContent("Content")
```

```
+----------+
| Content  |
+----------+
```

### None

No border (content only):

```csharp
new PanelNode()
    .WithBorder(BorderStyle.None)
    .WithContent("Content")
```

## Border Colors

```csharp
new PanelNode()
    .WithBorder(BorderStyle.Rounded)
    .WithBorderColor(Color.Cyan)
    .WithContent("Styled")
```

## Title Colors

Panels can have colored titles:

```csharp
new PanelNode()
    .WithTitle("My Panel")
    .WithBorderColor(Color.Gray)
    .WithTitleColor(Color.BrightCyan)  // Title stands out
    .WithContent("Content")
```

## Padding

Add inner padding between border and content:

```csharp
new PanelNode()
    .WithBorder(BorderStyle.Single)
    .WithPadding(1)  // 1 character padding on all sides
    .WithContent("Padded content")
```

## Common Patterns

### Information Panel

```csharp
new PanelNode()
    .WithTitle("Info")
    .WithBorder(BorderStyle.Rounded)
    .WithBorderColor(Color.Blue)
    .WithTitleColor(Color.BrightBlue)
    .WithContent(content)
```

### Warning Panel

```csharp
new PanelNode()
    .WithTitle("⚠ Warning")
    .WithBorder(BorderStyle.Double)
    .WithBorderColor(Color.Yellow)
    .WithTitleColor(Color.BrightYellow)
    .WithContent(warningMessage)
```

### Error Panel

```csharp
new PanelNode()
    .WithTitle("Error")
    .WithBorder(BorderStyle.Double)
    .WithBorderColor(Color.Red)
    .WithTitleColor(Color.BrightRed)
    .WithContent(errorMessage)
```

### Primary Content

```csharp
new PanelNode()
    .WithTitle("Main Content")
    .WithBorder(BorderStyle.Rounded)
    .WithBorderColor(Color.Cyan)
    .WithTitleColor(Color.BrightCyan)
    .WithContent(mainContent)
    .Fill()  // Take available space
```

## BorderStyle Enum

| Style | Characters | Use Case |
|-------|------------|----------|
| `Single` | ┌─┐│└┘ | Default, general use |
| `Double` | ╔═╗║╚╝ | Emphasis, dialogs |
| `Rounded` | ╭─╮│╰╯ | Modern, friendly look |
| `Ascii` | +-+\|+- | Terminal compatibility |
| `None` | (empty) | Content without border |

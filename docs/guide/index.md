# Introduction

Termina is a reactive terminal UI (TUI) framework for .NET that brings modern application development patterns to console applications.

## What is Termina?

Termina provides:

- **Reactive MVVM Architecture** - ViewModels with source-generated observable properties that automatically update the UI
- **Declarative Layout System** - Build UIs by composing layout nodes in a tree structure
- **ASP.NET Core-Style Routing** - Navigate between pages using familiar route templates
- **Direct ANSI Rendering** - Custom terminal rendering engine with surgical region-based updates

## Key Concepts

### Pages and ViewModels

Termina uses the MVVM (Model-View-ViewModel) pattern:

- **Pages** define the UI layout and bind to ViewModel properties
- **ViewModels** contain the application logic and state
- When ViewModel properties change, the UI automatically updates

### Reactive Properties

The `[Reactive]` attribute generates observable properties via source generators:

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    [Reactive] private string _message = "Hello";

    // Source generator creates:
    // - public string Message { get; set; }
    // - public IObservable<string> MessageChanged
}
```

### Layout Nodes

UIs are built by composing layout nodes:

```csharp
protected override ILayoutNode BuildLayout()
{
    return Layouts.Vertical()
        .WithChild(new TextNode("Header").Bold())
        .WithChild(new PanelNode().WithContent(new TextNode("Content")))
        .WithChild(new TextNode("Footer"));
}
```

### Routing

Navigate between pages using route templates:

```csharp
// Register routes
termina.RegisterRoute<HomePage, HomeViewModel>("/");
termina.RegisterRoute<TaskPage, TaskViewModel>("/tasks/{id:int}");

// Navigate
Navigate("/tasks/42");
```

## When to Use Termina

Termina is ideal for:

- **Interactive CLI tools** that need rich user interfaces
- **Dashboard applications** with multiple updating regions
- **Chat interfaces** with streaming text
- **Data entry forms** in terminal environments
- **Development tools** that run in the terminal

## Comparison with Alternatives

| Feature | Termina | Spectre.Console | Terminal.Gui |
|---------|---------|-----------------|--------------|
| Architecture | Reactive MVVM | Procedural | Imperative widgets |
| UI Updates | Observable-driven | Manual | Event-based |
| Layout | Declarative tree | Render calls | Widget hierarchy |
| Routing | Built-in | None | None |
| AOT Support | Full | Full | Limited |

## Next Steps

- [Getting Started](/guide/getting-started) - Build your first Termina app
- [Installation](/guide/installation) - Package installation options
- [Upgrading to 0.11.0](/guide/upgrade-0.11) - Native selection, copy, paste, and input mode guidance
- [Counter Tutorial](/tutorials/counter-app) - Step-by-step tutorial

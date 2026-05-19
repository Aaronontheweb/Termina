---
layout: home

hero:
  name: Termina
  text: Reactive TUI Framework for .NET
  tagline: Build beautiful terminal applications with declarative layouts and reactive state management
  image:
    src: /termina-icon.png
    alt: Termina
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/Aaronontheweb/Termina

features:
  - icon: ⚡
    title: Reactive MVVM
    details: Source-generated [Reactive] properties with automatic UI updates via observables. No boilerplate required.
  - icon: 🎨
    title: Declarative Layouts
    details: Tree-based layout system with Vertical, Horizontal, Stack, and custom components. Build complex UIs with simple composition.
  - icon: 🛤️
    title: ASP.NET-Style Routing
    details: Type-safe route parameters with /tasks/{id:int} templates and [FromRoute] injection. Navigate like a web app.
  - icon: 💉
    title: DI Integration
    details: Full Microsoft.Extensions.DependencyInjection and Hosting integration. Works with your existing .NET patterns.
  - icon: 🚀
    title: AOT Compatible
    details: Source generators enable Native AOT publishing with zero reflection. Ship single-file executables.
  - icon: 🎯
    title: Direct ANSI Rendering
    details: Custom terminal rendering with surgical region-based updates. No external dependencies for rendering.
---

## Quick Install

::: code-group

```shell [.NET CLI]
dotnet add package Termina
```

```xml [PackageReference]
<PackageReference Include="Termina" Version="0.2.*" />
```

:::

<div style="margin-top: 1rem;">
  <a href="https://www.nuget.org/packages/Termina" target="_blank">
    <img src="https://img.shields.io/nuget/v/Termina?style=flat-square&logo=nuget&label=NuGet" alt="NuGet" />
  </a>
  <a href="https://www.nuget.org/packages/Termina" target="_blank" style="margin-left: 0.5rem;">
    <img src="https://img.shields.io/nuget/dt/Termina?style=flat-square&logo=nuget&label=Downloads" alt="Downloads" />
  </a>
</div>

## See It In Action

![Termina component gallery: keyboard-driven menus, live spinners, and selection lists](/gallery/hero-overview.gif)

*A guided tour of the Termina component gallery.*

Termina apps are fully interactive — reactive state, streaming text, and surgical region-based updates, all in the terminal.

![Streaming chat demo: tokens render one-by-one into a scrolling transcript](/gallery/streaming-chat.gif)

*Streaming text rendered token-by-token, with an interactive decision list.*

![Dashboard demo: a multi-panel grid layout with live-updating metrics](/gallery/grid-dashboard.gif)

*A live dashboard composed with `GridNode`.*

## Quick Example

```csharp
// Define a ViewModel with reactive properties
public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;

    public override void OnActivated()
    {
        Input.OfType<KeyPressed>()
            .Subscribe(key =>
            {
                if (key.KeyInfo.Key == ConsoleKey.UpArrow) Count++;
                if (key.KeyInfo.Key == ConsoleKey.DownArrow) Count--;
                if (key.KeyInfo.Key == ConsoleKey.Q) Shutdown();
            })
            .DisposeWith(Subscriptions);
    }
}

// Define a Page with declarative layout
public class CounterPage : ReactivePage<CounterViewModel>
{
    protected override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Counter")
                    .WithContent(
                        ViewModel.CountChanged
                            .Select(c => new TextNode($"Count: {c}").Bold())
                            .AsLayout()
                    )
            )
            .WithChild(
                new TextNode("Press ↑/↓ to change, Q to quit")
                    .WithForeground(Color.Gray)
            );
    }
}
```

## Why Termina?

Termina brings modern application development patterns to terminal UIs:

- **Reactive by default** - UI automatically updates when state changes
- **Familiar patterns** - If you know ASP.NET Core or WPF/MAUI, you'll feel at home
- **Testable** - VirtualInputSource lets you write automated tests for your TUI
- **Performant** - Only changed regions are re-rendered, not the entire screen
- **Production-ready** - AOT compilation, proper error handling, async support

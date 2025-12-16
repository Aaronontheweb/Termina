# Architecture

Termina uses a reactive MVVM architecture with declarative layouts and surgical region-based rendering.

## Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Application                          │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌──────────────┐    ┌───────────────┐  │
│  │  ViewModel  │───▶│    Page      │───▶│  Layout Tree  │  │
│  │  [Reactive] │    │ BuildLayout()│    │  (ILayoutNode)│  │
│  └─────────────┘    └──────────────┘    └───────────────┘  │
│         ▲                                       │          │
│         │                                       ▼          │
│  ┌─────────────┐                        ┌───────────────┐  │
│  │    Input    │                        │   Renderer    │  │
│  │   Handler   │                        │  (ANSI out)   │  │
│  └─────────────┘                        └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Design Principles

### 1. Separation of Concerns

- **ViewModels** own state and handle input
- **Pages** build layouts from state
- **Layout Nodes** render to terminal

### 2. Reactive by Default

State changes automatically propagate to the UI:

```csharp
// ViewModel changes state
Count++;  // Sets Count, emits to CountChanged

// Page automatically updates
ViewModel.CountChanged
    .Select(c => new TextNode($"Count: {c}"))
    .AsLayout()  // Subscribes and updates on change
```

### 3. Declarative Layouts

UI is described as a tree, not imperatively drawn:

```csharp
return Layouts.Vertical()
    .WithChild(header.Height(1))
    .WithChild(content.Fill())
    .WithChild(footer.Height(1));
```

### 4. Region-Based Rendering

Only changed regions are re-rendered, not the entire screen. This provides:
- Smooth updates without flicker
- Efficient use of terminal bandwidth
- Better performance for streaming content

## Component Lifecycle

### Application Start

1. Host builds and starts
2. Router matches initial route
3. Page/ViewModel pair created via DI
4. ViewModel's `OnActivated()` called
5. Page's `BuildLayout()` called
6. Initial render to terminal

### On Input

1. Key press captured
2. Input event sent to current ViewModel
3. ViewModel updates state
4. Reactive bindings emit new values
5. Affected layout nodes invalidate
6. Changed regions re-render

### On Navigation

1. `Navigate("/path")` called
2. Current ViewModel's `OnDeactivating()` called
3. New Page/ViewModel resolved
4. New ViewModel's `OnActivated()` called
5. Full layout rebuild and render

## The Rendering Pipeline

```
BuildLayout() → Measure() → Render()
      │             │            │
      ▼             ▼            ▼
   Tree of      Calculate    Write ANSI
  ILayoutNode    sizes       to stdout
```

### 1. BuildLayout

Creates the layout tree describing the UI structure.

### 2. Measure

Starting from root, each node calculates its desired size given available space. Two-pass algorithm for container nodes:
1. Measure fixed/auto children
2. Distribute remaining space to fill children

### 3. Render

Each node renders to its allocated bounds using ANSI escape sequences.

## Why No Spectre.Console?

Termina V3 renders directly via ANSI escape sequences instead of using Spectre.Console. Benefits:

1. **Surgical Updates** - Re-render only changed regions
2. **True Streaming** - Character-by-character updates
3. **Full Control** - Custom rendering optimization
4. **AOT Compatible** - No reflection-based layout

## Source Code

::: details View ReactiveViewModel implementation
<<< @/../src/Termina/Reactive/ReactiveViewModel.cs{csharp}
:::

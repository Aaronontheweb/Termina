# Architecture

Termina uses a reactive MVVM architecture with declarative layouts and surgical region-based rendering.

## Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Application                          │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌──────────────┐    ┌───────────────┐  │
│  │  ViewModel  │───▶│    Page      │───▶│  Layout Tree  │  │
│  │ Input,State │    │ Focus,Layout │    │  (ILayoutNode)│  │
│  └─────────────┘    └──────────────┘    └───────────────┘  │
│         ▲                  │                    │          │
│         │                  ▼                    ▼          │
│  ┌─────────────┐    ┌──────────────┐    ┌───────────────┐  │
│  │   Keyboard  │    │    Focus     │    │   Renderer    │  │
│  │    Input    │    │   Manager    │    │  (ANSI out)   │  │
│  └─────────────┘    └──────────────┘    └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Design Principles

### 1. Separation of Concerns

- **ViewModels** own state, handle keyboard input, and expose observable properties
- **Pages** own focus management, build layouts from ViewModel state, and manage interactive layout nodes (modals, text inputs)
- **Layout Nodes** render to terminal and may handle routed input when focused

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

1. Key press captured by application
2. Input event sent to ViewModel's `Input` observable
3. ViewModel subscribes and handles input (updates state, navigates, etc.)
4. For focused interactive nodes (modals, text inputs), Page can route input via `Focus.RouteInput()`
5. Reactive bindings emit new values from state changes
6. Affected layout nodes invalidate
7. Changed regions re-render

### On Navigation

**ResetOnNavigation (default):**
1. `Navigate("/path")` called
2. Current ViewModel's `OnDeactivating()` called
3. Current Page's `OnNavigatingFrom()` called (deactivates layout)
4. New Page/ViewModel created
5. New ViewModel's `OnActivated()` called
6. New Page's `OnNavigatedTo()` called (builds and activates layout)
7. Full render

**PreserveState:**
1. `Navigate("/path")` called
2. Current ViewModel's `OnDeactivating()` called (disposes subscriptions)
3. Current Page's `OnNavigatingFrom()` called (calls `OnDeactivate()` on layout tree)
4. Cached Page/ViewModel retrieved (or created on first visit)
5. ViewModel's `OnActivated()` called (recreates subscriptions)
6. Page's `OnNavigatedTo()` called (calls `OnActivate()` on layout tree)
7. Render (layout preserved, just reactivated)

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

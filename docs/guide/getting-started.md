# Getting Started

This guide will walk you through creating your first Termina application.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- A terminal emulator (Windows Terminal, iTerm2, or any terminal with ANSI support)

## Create a New Project

```bash
# Create a new console application
dotnet new console -n MyTerminaApp
cd MyTerminaApp

# Add Termina package
dotnet add package Termina
```

## Understanding the Pattern

Termina uses an MVVM pattern with three key pieces:

1. **ViewModel** - Manages state with `[Reactive]` properties
2. **Page** - Builds the UI layout from state
3. **Host** - Wires everything together with routing

## Example: Counter Demo

Here's a complete working example from the Termina demos.

### The ViewModel

The ViewModel manages state with `[Reactive]` properties that automatically generate observable change notifications:

<<< @/../demos/Termina.Demo.RegionBased/CounterViewModel.cs{csharp}

::: tip The [Reactive] Attribute
The `[Reactive]` attribute uses source generation to create:
- A public property (e.g., `Count` from `_count`)
- An `IObservable<T>` property (e.g., `CountChanged`)
- Automatic change notifications

Your class must be `partial` for the source generator to work.
:::

### The Page

The Page builds the UI as a tree of layout nodes with reactive bindings:

<<< @/../demos/Termina.Demo.RegionBased/CounterPage.cs{csharp}

### The Program

Wire it all together with hosting and routing:

<<< @/../demos/Termina.Demo.RegionBased/Program.cs{csharp}

## Run the Application

```bash
dotnet run
```

You should see a terminal UI with a counter. Press ↑ to increment, ↓ to decrement, type to add messages, and Escape to quit.

## What Just Happened?

1. **Host Builder** - We used `Microsoft.Extensions.Hosting` for application lifecycle management
2. **AddTermina** - Registered Termina with the starting route `/counter`
3. **RegisterRoute** - Associated the route with our Page and ViewModel
4. **BuildLayout** - Defined our UI as a tree of layout nodes
5. **Reactive Binding** - `CountChanged.Select(...).AsLayout()` automatically updates the UI when `Count` changes

## Next Steps

- [Installation](/guide/installation) - Learn about package options and dependencies
- [Layout System](/layout/) - Understand how layouts work
- [Components](/components/) - Explore all available UI components
- [Counter Tutorial](/tutorials/counter-app) - Detailed walkthrough with more features

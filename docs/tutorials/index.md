# Tutorials

Learn Termina by building complete applications from scratch. Each tutorial builds on concepts from the previous one.

## Available Tutorials

### [Counter App](/tutorials/counter-app)

**Beginner** | ~15 minutes

Build your first Termina application - a reactive counter with keyboard input:

- Setting up a Termina project
- Creating ViewModels with `[Reactive]` properties
- Building layouts with panels and text nodes
- Handling keyboard input
- Reactive UI bindings

### [Todo List](/tutorials/todo-list)

**Intermediate** | ~30 minutes

Build a fully-featured todo list manager:

- Managing lists as reactive state
- List selection and navigation
- Adding, completing, and deleting items
- Text input handling
- Multi-page navigation

### [Streaming Chat](/tutorials/streaming-chat)

**Advanced** | ~45 minutes

Build a streaming chat interface with Akka.NET:

- Streaming text updates
- Text input components
- Async data sources with `IAsyncEnumerable`
- Akka.NET actor integration
- Cancellation and error handling

## Prerequisites

Before starting these tutorials, you should:

1. Have .NET 10.0 SDK installed
2. Be familiar with C# basics
3. Understand reactive programming concepts (see [Observables](/concepts/observables))

## Running the Demos

All tutorials are based on working demos in the repository:

```bash
# Clone the repository
git clone https://github.com/Aaronontheweb/Termina.git
cd Termina

# Run the counter demo
dotnet run --project demos/Termina.Demo.RegionBased

# Run the todo list demo
dotnet run --project demos/Termina.Demo

# Run the streaming chat demo
dotnet run --project demos/Termina.Demo.Streaming
```

## Next Steps

After completing these tutorials:

- Explore the [Component Library](/components/) for all available nodes
- Read [Custom Components](/advanced/custom-components) to build your own
- See [Testing](/advanced/testing) for automated testing patterns

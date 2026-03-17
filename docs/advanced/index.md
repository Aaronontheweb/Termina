# Advanced Topics

This section covers advanced Termina topics for building production-quality applications.

## Topics

### [Diagnostic Tracing](/advanced/diagnostics)

Debug and monitor your applications with Termina's built-in tracing system. Zero-cost when disabled, with category and level filtering.

### [Testing](/advanced/testing)

Learn to write automated tests for your Termina applications using `VirtualInputSource` to simulate user input without a real terminal.

### [Custom Components](/advanced/custom-components)

Build reusable custom layout nodes that integrate with Termina's rendering and measurement systems.

### [Clipboard And Feedback](/advanced/clipboard-and-feedback)

Understand Termina's terminal-native clipboard transports, toast notifications, and inline copy feedback patterns.

### [AOT Compilation](/advanced/aot)

Configure your Termina application for Native AOT publishing, enabling single-file executables with fast startup.

### [Akka.NET Integration](/advanced/akka-integration)

Patterns for integrating Akka.NET actors with Termina for streaming data, background processing, and distributed applications.

## Architecture Deep Dive

For a thorough understanding of Termina's internals, see the [Architecture](/concepts/architecture) documentation covering:

- The reactive MVVM pattern
- Layout measurement and rendering pipeline
- Region-based update system
- ANSI terminal abstraction

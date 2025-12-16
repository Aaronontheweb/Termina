# Comparisons

Understanding how Termina compares to other .NET terminal UI frameworks helps you choose the right tool for your project.

## Quick Comparison

| Feature | Termina | Spectre.Console | Terminal.Gui |
|---------|---------|-----------------|--------------|
| **Architecture** | Reactive MVVM | Imperative/Markup | Event-driven |
| **Layout** | Declarative tree | Widget-based | Container-based |
| **Updates** | Surgical regions | Full repaint | Partial repaint |
| **AOT Support** | Full (source gen) | Limited | Limited |
| **State Management** | Observable properties | Manual | Properties + events |
| **Streaming** | Native support | Limited | Manual |
| **Learning Curve** | Medium (Rx) | Low | Medium |

## Detailed Comparisons

### [vs Spectre.Console](/comparison/vs-spectre-console)

Spectre.Console excels at rich console output, prompts, and tables. Compare approaches for interactive applications.

### [vs Terminal.Gui](/comparison/vs-terminal-gui)

Terminal.Gui provides a traditional widget toolkit with windows, dialogs, and controls. Compare architectural approaches.

## When to Choose Termina

**Choose Termina when:**

- Building reactive, data-driven UIs
- Streaming content (LLM output, logs, real-time data)
- Need AOT compilation support
- Want declarative, composable layouts
- Integrating with Rx or Akka.NET

**Consider alternatives when:**

- Need simple prompts and confirmations (Spectre.Console)
- Building traditional dialog-based apps (Terminal.Gui)
- Require rich tables and charts (Spectre.Console)
- Need Windows-style TUI (Terminal.Gui)

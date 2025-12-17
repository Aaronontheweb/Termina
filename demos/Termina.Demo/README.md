# Termina Demo

A demonstration application showcasing Termina's reactive TUI capabilities, including:

- Counter page with reactive state
- Todo list with modals and focus management
- Navigation between pages
- Diagnostic tracing integration

## Running the Demo

### Interactive Mode

Run the demo interactively to explore the UI:

```bash
dotnet run --project demos/Termina.Demo
```

**Controls:**

Counter Page:
- `↑` / `↓` - Increment/decrement counter
- `R` - Reset counter
- `T` - Navigate to todo list
- `Q` - Quit

Todo List Page:
- `↑` / `↓` - Navigate items
- `Space` - Toggle completion
- `A` - Add new item (opens modal)
- `D` - Delete selected item
- `C` - Navigate to counter
- `Q` - Quit

### Headless/CI Mode

Run with `--test` flag for automated testing without user interaction:

```bash
dotnet run --project demos/Termina.Demo -- --test
```

This mode:
- Uses `VirtualInputSource` to simulate keystrokes
- Runs a scripted test sequence (increment counter, navigate to todos, toggle an item)
- Exits automatically after completing the test sequence
- Useful for CI/CD pipelines to verify the app starts and renders correctly

## Diagnostic Tracing

The demo automatically enables diagnostic tracing to help debug and understand Termina's internals.

### Log File Location

Trace logs are written to:
```
%TEMP%\termina-logs\trace-{timestamp}.log
```

For example: `C:\Users\you\AppData\Local\Temp\termina-logs\trace-20251217-133215.log`

The trace file path is displayed at the bottom of both the Counter and Todo List pages.

### Log Contents

The trace logs capture:
- **Page** - Navigation events, page lifecycle (creation, caching, activation)
- **Input** - Input source startup and key events
- **Render** - Screen mode changes, cursor visibility
- **Focus** - Focus stack operations (when applicable)

### Example Output

```
2025-12-17 13:32:15.390 [INFO] [Page] TerminaApplication#00D45D21 - Navigating to: /counter
2025-12-17 13:32:15.391 [DEBUG] [Page] TerminaApplication#00D45D21 - Creating new page, behavior=PreserveState
2025-12-17 13:32:15.399 [DEBUG] [Page] TerminaApplication#00D45D21 - Cached page: /counter
2025-12-17 13:32:15.411 [INFO] [Page] TerminaApplication#00D45D21 - Navigation complete: /counter, page=CounterPage
2025-12-17 13:32:15.419 [INFO] [Page] TerminaApplication#00D45D21 - RunAsync starting
2025-12-17 13:32:15.422 [DEBUG] [Input] TerminaApplication#00D45D21 - Started 1 input source(s)
2025-12-17 13:32:15.422 [DEBUG] [Render] TerminaApplication#00D45D21 - Entered alternate screen, cursor hidden
```

### Viewing Logs in Real-Time

On Windows, you can tail the log file while the demo runs:

```powershell
# PowerShell
Get-Content -Path "$env:TEMP\termina-logs\trace-*.log" -Tail 20 -Wait
```

On Unix-like systems:

```bash
tail -f /tmp/termina-logs/trace-*.log
```

## Configuration

The demo configures tracing in `Program.cs`:

```csharp
// Set up diagnostic tracing with timestamped log file in temp directory
var traceDir = Path.Combine(Path.GetTempPath(), "termina-logs");
Directory.CreateDirectory(traceDir);
var traceFile = Path.Combine(traceDir, $"trace-{DateTime.Now:yyyyMMdd-HHmmss}.log");

builder.Services.AddTerminaFileTracing(traceFile, TerminaTraceCategory.All, TerminaTraceLevel.Debug);
```

You can modify the trace categories and levels as needed. See the [Diagnostic Tracing documentation](/docs/advanced/diagnostics.md) for details.

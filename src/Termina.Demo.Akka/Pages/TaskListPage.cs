using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Demo.Akka.Actors;
using Termina.Input;
using Termina.Pages;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// Task list page displaying tasks with timers.
/// Demonstrates live updates from Akka.NET actors.
/// </summary>
public sealed class TaskListPage : PageBase<TaskListUIEvent, TaskListCommand>
{
    private IReadOnlyList<TaskItem> _tasks = Array.Empty<TaskItem>();
    private int _selectedIndex = 0;
    private string _statusMessage = "";
    private bool _isAddingTask = false;
    private string _newTaskInput = "";

    public override IEnumerable<Component> Components => [];

    protected override TaskListUIEvent? MapToUIEvent(IInputEvent raw)
    {
        if (raw is not KeyPressed key) return null;

        // When adding a task, handle text input mode
        if (_isAddingTask)
        {
            switch (key.KeyInfo.Key)
            {
                case ConsoleKey.Escape:
                    _isAddingTask = false;
                    _newTaskInput = "";
                    return null;

                case ConsoleKey.Enter when _newTaskInput.Length > 0:
                    var description = _newTaskInput;
                    _newTaskInput = "";
                    _isAddingTask = false;
                    return new TaskListUIEvent.AddTaskRequested(description);

                case ConsoleKey.Backspace when _newTaskInput.Length > 0:
                    _newTaskInput = _newTaskInput[..^1];
                    return null;

                default:
                    // Add printable character
                    if (!char.IsControl(key.KeyInfo.KeyChar))
                    {
                        _newTaskInput += key.KeyInfo.KeyChar;
                    }
                    return null;
            }
        }

        // Normal mode
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                if (_tasks.Count > 0)
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                return null;

            case ConsoleKey.DownArrow:
                if (_tasks.Count > 0)
                    _selectedIndex = Math.Min(_tasks.Count - 1, _selectedIndex + 1);
                return null;

            case ConsoleKey.A:
            case ConsoleKey.N:
                _isAddingTask = true;
                _newTaskInput = "";
                return null;

            case ConsoleKey.D:
            case ConsoleKey.Delete:
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                    return new TaskListUIEvent.RemoveTaskRequested(_tasks[_selectedIndex].Id);
                return null;

            case ConsoleKey.Spacebar:
            case ConsoleKey.Enter:
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                    return new TaskListUIEvent.ToggleTaskRequested(_tasks[_selectedIndex].Id);
                return null;

            case ConsoleKey.S:
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                {
                    var task = _tasks[_selectedIndex];
                    return task.IsTimerRunning
                        ? new TaskListUIEvent.StopTimerRequested(task.Id)
                        : new TaskListUIEvent.StartTimerRequested(task.Id);
                }
                return null;

            case ConsoleKey.Escape:
            case ConsoleKey.Q:
                return new TaskListUIEvent.BackRequested();

            default:
                return null;
        }
    }

    protected override void ApplyCommand(TaskListCommand command)
    {
        switch (command)
        {
            case TaskListCommand.UpdateTaskList(var tasks):
                _tasks = tasks;
                // Adjust selection if needed
                if (_selectedIndex >= _tasks.Count)
                    _selectedIndex = Math.Max(0, _tasks.Count - 1);
                break;

            case TaskListCommand.UpdateTaskTimer(var taskId, var elapsed):
                // Find and update the specific task's timer display
                // This is handled by the full list update for now
                break;

            case TaskListCommand.ShowStatus(var message):
                _statusMessage = message;
                break;

            case TaskListCommand.EnterAddTaskMode:
                _isAddingTask = true;
                _newTaskInput = "";
                break;

            case TaskListCommand.ExitAddTaskMode:
                _isAddingTask = false;
                _newTaskInput = "";
                break;
        }
    }

    public override IRenderable Render()
    {
        var rows = new List<IRenderable>();

        // Header
        rows.Add(new Rule("[bold blue]Task Manager with Live Timers[/]").LeftJustified());
        rows.Add(new Text(""));

        // Add task input mode
        if (_isAddingTask)
        {
            rows.Add(new Panel(
                new Markup($"[yellow]New task:[/] {Markup.Escape(_newTaskInput)}[blink]|[/]"))
                .Border(BoxBorder.Rounded)
                .Header("[yellow]Adding Task[/]"));
            rows.Add(new Text(""));
            rows.Add(new Markup("[grey]Press Enter to add, Escape to cancel[/]"));
        }
        else
        {
            // Task list
            if (_tasks.Count == 0)
            {
                rows.Add(new Panel(new Markup("[grey italic]No tasks yet. Press 'A' to add a task.[/]"))
                    .Border(BoxBorder.Rounded)
                    .Header("Tasks"));
            }
            else
            {
                var taskRows = new List<IRenderable>();
                for (int i = 0; i < _tasks.Count; i++)
                {
                    var task = _tasks[i];
                    var isSelected = i == _selectedIndex;
                    var escapedDesc = Markup.Escape(task.Description);
                    if (string.IsNullOrEmpty(escapedDesc))
                        escapedDesc = "(untitled)";

                    // Build the line - use Text for simpler rendering to avoid markup issues
                    var prefix = isSelected ? "> " : "  ";
                    var checkbox = task.IsCompleted ? "[[x]]" : "[[ ]]";

                    var timerDisplay = "";
                    if (task.IsTimerRunning)
                    {
                        timerDisplay = $" ({FormatTime(task.ElapsedTime)} *)";
                    }
                    else if (task.ElapsedTime > TimeSpan.Zero)
                    {
                        timerDisplay = $" ({FormatTime(task.ElapsedTime)})";
                    }

                    var line = $"{prefix}{checkbox} {escapedDesc}{timerDisplay}";

                    // Use simple color styling
                    if (task.IsCompleted)
                    {
                        taskRows.Add(new Markup($"[dim]{line}[/]"));
                    }
                    else if (isSelected)
                    {
                        taskRows.Add(new Markup($"[bold blue]{line}[/]"));
                    }
                    else
                    {
                        taskRows.Add(new Text(line));
                    }
                }

                rows.Add(new Panel(new Rows(taskRows))
                    .Border(BoxBorder.Rounded)
                    .Header($"Tasks ({_tasks.Count})"));
            }

            rows.Add(new Text(""));

            // Help text
            var helpTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn("Key")
                .AddColumn("Action");

            helpTable.AddRow("[grey]↑/↓[/]", "[grey]Navigate[/]");
            helpTable.AddRow("[grey]Space/Enter[/]", "[grey]Toggle complete[/]");
            helpTable.AddRow("[grey]A/N[/]", "[grey]Add task[/]");
            helpTable.AddRow("[grey]D/Del[/]", "[grey]Delete task[/]");
            helpTable.AddRow("[grey]S[/]", "[grey]Start/Stop timer[/]");
            helpTable.AddRow("[grey]Q/Esc[/]", "[grey]Exit[/]");

            rows.Add(helpTable);
        }

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            rows.Add(new Text(""));
            rows.Add(new Markup($"[yellow]{Markup.Escape(_statusMessage)}[/]"));
        }

        return new Rows(rows);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        return $"{time.Minutes}:{time.Seconds:D2}";
    }
}

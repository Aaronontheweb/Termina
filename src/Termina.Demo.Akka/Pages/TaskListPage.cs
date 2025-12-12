using Spectre.Console;
using Spectre.Console.Rendering;
using Termina;
using Termina.Demo.Akka.Actors;
using Termina.Input;
using Termina.Pages;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// Task list page displaying tasks with timers, priorities, and statistics.
/// Demonstrates live updates from Akka.NET actors with rich Spectre.Console UI.
/// </summary>
public sealed class TaskListPage : PageBase<TaskListUIEvent, TaskListCommand>
{
    private IReadOnlyList<TaskItem> _tasks = Array.Empty<TaskItem>();
    private TaskStatistics _stats = new(0, 0, 0, TimeSpan.Zero, 0, 0);
    private int _selectedIndex = 0;
    private string _statusMessage = "";
    private bool _isAddingTask = false;
    private string _newTaskInput = "";
    private TaskPriority _newTaskPriority = TaskPriority.Medium;

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
                    var priority = _newTaskPriority;
                    _newTaskInput = "";
                    _isAddingTask = false;
                    return new TaskListUIEvent.AddTaskRequested(description, priority);

                case ConsoleKey.Backspace when _newTaskInput.Length > 0:
                    _newTaskInput = _newTaskInput[..^1];
                    return null;

                case ConsoleKey.Tab:
                    // Cycle through priorities
                    _newTaskPriority = (TaskPriority)(((int)_newTaskPriority + 1) % 4);
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
            case ConsoleKey.K:
                if (_tasks.Count > 0)
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                return null;

            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                if (_tasks.Count > 0)
                    _selectedIndex = Math.Min(_tasks.Count - 1, _selectedIndex + 1);
                return null;

            case ConsoleKey.A:
            case ConsoleKey.N:
                _isAddingTask = true;
                _newTaskInput = "";
                _newTaskPriority = TaskPriority.Medium;
                return null;

            case ConsoleKey.D:
            case ConsoleKey.Delete:
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                    return new TaskListUIEvent.RemoveTaskRequested(_tasks[_selectedIndex].Id);
                return null;

            case ConsoleKey.Spacebar:
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                    return new TaskListUIEvent.ToggleTaskRequested(_tasks[_selectedIndex].Id);
                return null;

            case ConsoleKey.Enter:
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                    return new TaskListUIEvent.ViewDetailRequested(_tasks[_selectedIndex].Id);
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

            case ConsoleKey.P:
                // Cycle priority on selected task
                if (_tasks.Count > 0 && _selectedIndex < _tasks.Count)
                {
                    var task = _tasks[_selectedIndex];
                    var nextPriority = (TaskPriority)(((int)task.Priority + 1) % 4);
                    return new TaskListUIEvent.ChangePriorityRequested(task.Id, nextPriority);
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
            case TaskListCommand.UpdateTaskList(var tasks, var stats):
                _tasks = tasks;
                _stats = stats;
                // Adjust selection if needed
                if (_selectedIndex >= _tasks.Count)
                    _selectedIndex = Math.Max(0, _tasks.Count - 1);
                break;

            case TaskListCommand.UpdateTaskTimer(var taskId, var elapsed):
                // Handled by full list update
                break;

            case TaskListCommand.ShowStatus(var message):
                _statusMessage = message;
                break;

            case TaskListCommand.EnterAddTaskMode(var defaultPriority):
                _isAddingTask = true;
                _newTaskInput = "";
                _newTaskPriority = defaultPriority;
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

        // Header with title
        rows.Add(new Rule("[bold blue]Task Manager[/]").LeftJustified());
        rows.Add(new Text(""));

        // Statistics panel
        rows.Add(RenderStatisticsPanel());
        rows.Add(new Text(""));

        // Add task input mode
        if (_isAddingTask)
        {
            rows.Add(RenderAddTaskPanel());
        }
        else
        {
            // Task list
            rows.Add(RenderTaskList());
            rows.Add(new Text(""));

            // Help text
            rows.Add(RenderHelpPanel());
        }

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            rows.Add(new Text(""));
            rows.Add(new Text(_statusMessage, new Style(Color.Yellow)));
        }

        return new Rows(rows);
    }

    private IRenderable RenderStatisticsPanel()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        // Row 1: Completion progress
        var completionPercent = _stats.TotalTasks > 0
            ? (double)_stats.CompletedTasks / _stats.TotalTasks
            : 0;

        var completionBar = new ProgressBar
        {
            Value = completionPercent,
            MaxValue = 1.0,
            Width = 20
        };

        // Row 2: Summary stats
        var totalTime = FormatTime(_stats.TotalTimeTracked);

        grid.AddRow(
            new Markup($"[bold]Tasks:[/] {_stats.CompletedTasks}/{_stats.TotalTasks}"),
            new Markup($"[bold]Active:[/] [yellow]{_stats.ActiveTimers}[/] timers"),
            new Markup($"[bold]Time:[/] [cyan]{totalTime}[/]")
        );

        // Alert row if there are critical/high priority items
        if (_stats.CriticalPending > 0 || _stats.HighPriorityPending > 0)
        {
            var alerts = new List<string>();
            if (_stats.CriticalPending > 0)
                alerts.Add($"[bold red]{_stats.CriticalPending} CRITICAL[/]");
            if (_stats.HighPriorityPending > 0)
                alerts.Add($"[orange1]{_stats.HighPriorityPending} High[/]");

            grid.AddRow(
                new Markup($"[bold]Pending:[/] {string.Join(" | ", alerts)}"),
                new Text(""),
                new Text("")
            );
        }

        return new Panel(grid)
            .Header("[bold]Statistics[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);
    }

    private IRenderable RenderAddTaskPanel()
    {
        var priorityDisplay = GetPriorityMarkup(_newTaskPriority);
        var content = new Rows(
            new Markup($"[yellow]Description:[/] {Markup.Escape(_newTaskInput)}[blink]|[/]"),
            new Markup($"[yellow]Priority:[/] {priorityDisplay} [dim](Tab to change)[/]")
        );

        return new Panel(content)
            .Header("[yellow bold]New Task[/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Yellow);
    }

    private IRenderable RenderTaskList()
    {
        if (_tasks.Count == 0)
        {
            return new Panel(new Markup("[dim italic]No tasks yet. Press 'A' to add a task.[/]"))
                .Header("Tasks")
                .Border(BoxBorder.Rounded);
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("").Width(3))           // Selection
            .AddColumn(new TableColumn("").Width(3))           // Checkbox
            .AddColumn(new TableColumn("Pri").Centered())      // Priority
            .AddColumn(new TableColumn("Description"))         // Description
            .AddColumn(new TableColumn("Time").RightAligned()) // Time
            .AddColumn(new TableColumn("Progress").Width(12)); // Progress bar

        foreach (var (task, index) in _tasks.Select((t, i) => (t, i)))
        {
            var isSelected = index == _selectedIndex;
            var selector = isSelected ? "[bold blue]>[/]" : " ";
            var checkbox = task.IsCompleted ? "[green][[x]][/]" : "[dim][[ ]][/]";
            var priority = GetPriorityMarkup(task.Priority);

            // Description with strikethrough if completed
            var desc = Markup.Escape(task.Description);
            if (task.IsCompleted)
                desc = $"[strikethrough dim]{desc}[/]";
            else if (isSelected)
                desc = $"[bold]{desc}[/]";

            // Time display
            var timeDisplay = FormatTime(task.ElapsedTime);
            if (task.IsTimerRunning)
                timeDisplay = $"[yellow]{timeDisplay} *[/]";
            else if (task.ElapsedTime > TimeSpan.Zero)
                timeDisplay = $"[dim]{timeDisplay}[/]";
            else
                timeDisplay = "[dim]-[/]";

            // Progress bar (if has estimated duration)
            IRenderable progressCell;
            if (task.EstimatedDuration.HasValue && task.ElapsedTime > TimeSpan.Zero)
            {
                var progress = Math.Min(1.0, task.ElapsedTime / task.EstimatedDuration.Value);
                var color = progress switch
                {
                    >= 1.0 => Color.Red,
                    >= 0.75 => Color.Orange1,
                    >= 0.5 => Color.Yellow,
                    _ => Color.Green
                };
                progressCell = new ProgressBar
                {
                    Value = progress,
                    MaxValue = 1.0,
                    Width = 10,
                    CompletedStyle = new Style(color),
                    RemainingStyle = new Style(Color.Grey23)
                };
            }
            else
            {
                progressCell = new Text("-", new Style(Color.Grey));
            }

            table.AddRow(
                new Markup(selector),
                new Markup(checkbox),
                new Markup(priority),
                new Markup(desc),
                new Markup(timeDisplay),
                progressCell
            );
        }

        return table;
    }

    private IRenderable RenderHelpPanel()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            new Markup("[dim]↑/↓[/] Navigate"),
            new Markup("[dim]Space[/] Toggle"),
            new Markup("[dim]Enter[/] Details"),
            new Markup("[dim]A[/] Add task")
        );
        grid.AddRow(
            new Markup("[dim]S[/] Start/Stop"),
            new Markup("[dim]P[/] Priority"),
            new Markup("[dim]D[/] Delete"),
            new Markup("[dim]Q[/] Exit")
        );

        return grid;
    }

    private static string GetPriorityMarkup(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "[bold red]!!![/]",
        TaskPriority.High => "[orange1]!![/]",
        TaskPriority.Medium => "[yellow]![/]",
        TaskPriority.Low => "[dim]·[/]",
        _ => "[dim]?[/]"
    };

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        return $"0:{time.Seconds:D2}";
    }
}

/// <summary>
/// Simple progress bar widget for Spectre.Console.
/// </summary>
internal sealed class ProgressBar : IRenderable
{
    public double Value { get; init; }
    public double MaxValue { get; init; } = 1.0;
    public int Width { get; init; } = 20;
    public Style CompletedStyle { get; init; } = new(Color.Green);
    public Style RemainingStyle { get; init; } = new(Color.Grey23);

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        return new Measurement(Width, Width);
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var effectiveWidth = Math.Min(Width, maxWidth);
        var percentage = MaxValue > 0 ? Math.Clamp(Value / MaxValue, 0, 1) : 0;
        var filledWidth = (int)(effectiveWidth * percentage);
        var emptyWidth = effectiveWidth - filledWidth;

        if (filledWidth > 0)
            yield return new Segment(new string('█', filledWidth), CompletedStyle);
        if (emptyWidth > 0)
            yield return new Segment(new string('░', emptyWidth), RemainingStyle);
    }
}

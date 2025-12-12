using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Demo.Akka.Actors;
using Termina.Input;
using Termina.Pages;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// Task detail page showing full information about a single task.
/// </summary>
public sealed class TaskDetailPage : PageBase<TaskDetailUIEvent, TaskDetailCommand>
{
    private TaskItem? _task;
    private string _statusMessage = "";

    public override IEnumerable<Component> Components => [];

    protected override TaskDetailUIEvent? MapToUIEvent(IInputEvent raw)
    {
        if (raw is not KeyPressed key) return null;

        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.S:
                return new TaskDetailUIEvent.ToggleTimerRequested();

            case ConsoleKey.Spacebar:
            case ConsoleKey.C:
                return new TaskDetailUIEvent.ToggleCompletionRequested();

            case ConsoleKey.P:
                // Cycle to next priority
                if (_task != null)
                {
                    var nextPriority = (TaskPriority)(((int)_task.Priority + 1) % 4);
                    return new TaskDetailUIEvent.ChangePriorityRequested(nextPriority);
                }
                return null;

            case ConsoleKey.Escape:
            case ConsoleKey.Q:
            case ConsoleKey.Backspace:
                return new TaskDetailUIEvent.BackRequested();

            default:
                return null;
        }
    }

    protected override void ApplyCommand(TaskDetailCommand command)
    {
        switch (command)
        {
            case TaskDetailCommand.UpdateTask(var task):
                _task = task;
                break;

            case TaskDetailCommand.ShowStatus(var message):
                _statusMessage = message;
                break;
        }
    }

    public override IRenderable Render()
    {
        if (_task == null)
        {
            return new Panel(new Markup("[dim italic]Loading task details...[/]"))
                .Header("Task Detail")
                .Border(BoxBorder.Rounded);
        }

        var rows = new List<IRenderable>();

        // Header with task ID
        rows.Add(new Rule($"[bold blue]Task #{_task.Id}[/]").LeftJustified());
        rows.Add(new Text(""));

        // Main info panel
        rows.Add(RenderInfoPanel());
        rows.Add(new Text(""));

        // Time tracking panel
        rows.Add(RenderTimePanel());
        rows.Add(new Text(""));

        // Help text
        rows.Add(RenderHelpPanel());

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            rows.Add(new Text(""));
            rows.Add(new Text(_statusMessage, new Style(Color.Yellow)));
        }

        return new Rows(rows);
    }

    private IRenderable RenderInfoPanel()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        // Description
        grid.AddRow(
            new Markup("[bold]Description:[/]"),
            new Markup(Markup.Escape(_task!.Description))
        );

        // Priority with color
        var priorityMarkup = _task.Priority switch
        {
            TaskPriority.Critical => "[bold red]CRITICAL[/]",
            TaskPriority.High => "[orange1]High[/]",
            TaskPriority.Medium => "[yellow]Medium[/]",
            TaskPriority.Low => "[dim]Low[/]",
            _ => "[dim]Unknown[/]"
        };
        grid.AddRow(
            new Markup("[bold]Priority:[/]"),
            new Markup(priorityMarkup)
        );

        // Status
        var statusMarkup = _task.IsCompleted
            ? "[green]Completed[/]"
            : "[yellow]Pending[/]";
        grid.AddRow(
            new Markup("[bold]Status:[/]"),
            new Markup(statusMarkup)
        );

        // Created at
        var createdAgo = DateTime.UtcNow - _task.CreatedAt;
        var createdDisplay = createdAgo.TotalMinutes < 1
            ? "Just now"
            : createdAgo.TotalHours < 1
                ? $"{(int)createdAgo.TotalMinutes} minutes ago"
                : createdAgo.TotalDays < 1
                    ? $"{(int)createdAgo.TotalHours} hours ago"
                    : $"{(int)createdAgo.TotalDays} days ago";

        grid.AddRow(
            new Markup("[bold]Created:[/]"),
            new Markup($"[dim]{createdDisplay}[/]")
        );

        return new Panel(grid)
            .Header("[bold]Task Information[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);
    }

    private IRenderable RenderTimePanel()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        // Timer status
        var timerStatus = _task!.IsTimerRunning
            ? "[green bold]RUNNING[/]"
            : "[dim]Stopped[/]";

        // Elapsed time
        var elapsed = FormatTime(_task.ElapsedTime);
        var elapsedMarkup = _task.IsTimerRunning
            ? $"[yellow]{elapsed}[/]"
            : $"[dim]{elapsed}[/]";

        // Estimated duration
        var estimated = _task.EstimatedDuration.HasValue
            ? FormatTime(_task.EstimatedDuration.Value)
            : "-";

        grid.AddRow(
            new Markup($"[bold]Timer:[/] {timerStatus}"),
            new Markup($"[bold]Elapsed:[/] {elapsedMarkup}"),
            new Markup($"[bold]Estimated:[/] [dim]{estimated}[/]")
        );

        // Progress bar if we have an estimate
        if (_task.EstimatedDuration.HasValue && _task.ElapsedTime > TimeSpan.Zero)
        {
            var progress = Math.Min(1.0, _task.ElapsedTime / _task.EstimatedDuration.Value);
            var percentage = (int)(progress * 100);
            var color = progress switch
            {
                >= 1.0 => Color.Red,
                >= 0.75 => Color.Orange1,
                >= 0.5 => Color.Yellow,
                _ => Color.Green
            };

            var barWidth = 40;
            var filledWidth = (int)(barWidth * progress);
            var emptyWidth = barWidth - filledWidth;

            var progressBar = new Markup(
                $"[{color}]{new string('█', filledWidth)}[/][grey23]{new string('░', emptyWidth)}[/] {percentage}%"
            );

            grid.AddEmptyRow();
            grid.AddRow(
                new Markup("[bold]Progress:[/]"),
                progressBar,
                new Text("")
            );
        }

        return new Panel(grid)
            .Header("[bold]Time Tracking[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1);
    }

    private IRenderable RenderHelpPanel()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            new Markup("[dim]S[/] Toggle timer"),
            new Markup("[dim]Space/C[/] Toggle done"),
            new Markup("[dim]P[/] Change priority"),
            new Markup("[dim]Q/Esc[/] Back")
        );

        return grid;
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        return $"0:{time.Seconds:D2}";
    }
}

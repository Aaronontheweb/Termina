using Termina.Events;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// UI events for the task list page.
/// Transformed from raw input by the page.
/// </summary>
public abstract record TaskListUIEvent : IPageUIEvent
{
    /// <summary>
    /// User wants to add a new task.
    /// </summary>
    public sealed record AddTaskRequested(string Description) : TaskListUIEvent;

    /// <summary>
    /// User wants to remove a task.
    /// </summary>
    public sealed record RemoveTaskRequested(int TaskId) : TaskListUIEvent;

    /// <summary>
    /// User wants to toggle a task's completed status.
    /// </summary>
    public sealed record ToggleTaskRequested(int TaskId) : TaskListUIEvent;

    /// <summary>
    /// User wants to start a task's timer.
    /// </summary>
    public sealed record StartTimerRequested(int TaskId) : TaskListUIEvent;

    /// <summary>
    /// User wants to stop a task's timer.
    /// </summary>
    public sealed record StopTimerRequested(int TaskId) : TaskListUIEvent;

    /// <summary>
    /// User pressed escape to go back/exit.
    /// </summary>
    public sealed record BackRequested : TaskListUIEvent;
}

/// <summary>
/// Commands sent from handler to task list page.
/// </summary>
public abstract record TaskListCommand : IUICommand
{
    /// <summary>
    /// Update the displayed task list.
    /// </summary>
    public sealed record UpdateTaskList(IReadOnlyList<Actors.TaskItem> Tasks) : TaskListCommand;

    /// <summary>
    /// Update a single task's timer display.
    /// </summary>
    public sealed record UpdateTaskTimer(int TaskId, TimeSpan Elapsed) : TaskListCommand;

    /// <summary>
    /// Show a status message.
    /// </summary>
    public sealed record ShowStatus(string Message) : TaskListCommand;

    /// <summary>
    /// Enter add task mode.
    /// </summary>
    public sealed record EnterAddTaskMode : TaskListCommand;

    /// <summary>
    /// Exit add task mode.
    /// </summary>
    public sealed record ExitAddTaskMode : TaskListCommand;
}

using Termina.Demo.Akka.Actors;
using Termina.Events;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// UI events for the task detail page.
/// </summary>
public abstract record TaskDetailUIEvent : IPageUIEvent
{
    /// <summary>
    /// User wants to toggle timer.
    /// </summary>
    public sealed record ToggleTimerRequested : TaskDetailUIEvent;

    /// <summary>
    /// User wants to change priority.
    /// </summary>
    public sealed record ChangePriorityRequested(TaskPriority Priority) : TaskDetailUIEvent;

    /// <summary>
    /// User wants to toggle completion.
    /// </summary>
    public sealed record ToggleCompletionRequested : TaskDetailUIEvent;

    /// <summary>
    /// User wants to go back to task list.
    /// </summary>
    public sealed record BackRequested : TaskDetailUIEvent;
}

/// <summary>
/// Commands sent from handler to task detail page.
/// </summary>
public abstract record TaskDetailCommand : IUICommand
{
    /// <summary>
    /// Update the displayed task.
    /// </summary>
    public sealed record UpdateTask(TaskItem Task) : TaskDetailCommand;

    /// <summary>
    /// Show a status message.
    /// </summary>
    public sealed record ShowStatus(string Message) : TaskDetailCommand;
}

using Akka.Actor;
using Termina.Demo.Akka.Actors;
using Termina.Events;
using Termina.Pages;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// Handler for the task list page.
/// Routes UI events to Akka.NET actors and model events to the page.
/// </summary>
public sealed class TaskListHandler
    : PageHandler<TaskListPage, TaskListUIEvent, TaskListCommand>
{
    private IActorRef? _taskManager;

    /// <summary>
    /// Sets the actor reference for the task manager.
    /// Called during application setup.
    /// </summary>
    public void SetTaskManager(IActorRef taskManager)
    {
        _taskManager = taskManager;
    }

    protected override void OnNavigatedTo()
    {
        // The actor will send TaskListUpdated on startup, so we'll get initial state
        Send(new TaskListCommand.ShowStatus("Connected to Task Manager"));
    }

    protected override void HandleUIEvent(TaskListUIEvent evt)
    {
        if (_taskManager == null)
        {
            Send(new TaskListCommand.ShowStatus("Error: Task manager not connected"));
            return;
        }

        switch (evt)
        {
            case TaskListUIEvent.AddTaskRequested(var description):
                _taskManager.Tell(new AddTask(description));
                Send(new TaskListCommand.ShowStatus($"Adding task: {description}"));
                break;

            case TaskListUIEvent.RemoveTaskRequested(var taskId):
                _taskManager.Tell(new RemoveTask(taskId));
                break;

            case TaskListUIEvent.ToggleTaskRequested(var taskId):
                _taskManager.Tell(new ToggleTask(taskId));
                break;

            case TaskListUIEvent.StartTimerRequested(var taskId):
                _taskManager.Tell(new StartTimer(taskId));
                break;

            case TaskListUIEvent.StopTimerRequested(var taskId):
                _taskManager.Tell(new StopTimer(taskId));
                break;

            case TaskListUIEvent.BackRequested:
                Shutdown();
                break;
        }
    }

    protected override void HandleModelEvent(IModelEvent evt)
    {
        switch (evt)
        {
            case TaskListUpdated(var tasks):
                Send(new TaskListCommand.UpdateTaskList(tasks));
                Send(new TaskListCommand.ShowStatus("")); // Clear status
                break;

            case TaskTimerTick(var taskId, var elapsed):
                Send(new TaskListCommand.UpdateTaskTimer(taskId, elapsed));
                break;
        }
    }
}

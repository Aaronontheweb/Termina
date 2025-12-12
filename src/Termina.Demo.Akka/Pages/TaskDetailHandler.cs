using Akka.Actor;
using Akka.Hosting;
using Termina.Demo.Akka.Actors;
using Termina.Pages;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// Handler for the task detail page.
/// Injects actor reference via DI and manages communication with the actor.
/// </summary>
public sealed class TaskDetailHandler
    : PageHandler<TaskDetailPage, TaskDetailUIEvent, TaskDetailCommand>
{
    // Static pending task - set by TaskListHandler before navigation
    private static TaskItem? _pendingTask;

    private readonly IActorRef _taskManager;
    private readonly ActorSystem _actorSystem;
    private IActorRef? _subscriber;
    private TaskItem? _currentTask;

    public TaskDetailHandler(IRequiredActor<TaskManagerActor> taskManagerProvider, ActorSystem actorSystem)
    {
        _taskManager = taskManagerProvider.ActorRef;
        _actorSystem = actorSystem;
    }

    /// <summary>
    /// Sets the pending task data to be displayed when navigating to this page.
    /// Called by TaskListHandler before navigation.
    /// </summary>
    public static void SetPendingTask(TaskItem task) => _pendingTask = task;

    protected override void OnNavigatedTo()
    {
        // Pick up the pending task
        if (_pendingTask != null)
        {
            _currentTask = _pendingTask;
            _pendingTask = null;
            Send(new TaskDetailCommand.UpdateTask(_currentTask));
        }

        // Create subscriber actor for updates
        _subscriber = _actorSystem.ActorOf(Props.Create(() => new EventSubscriber(this)));

        // Subscribe to task list updates (to get updates for our task)
        _actorSystem.EventStream.Subscribe(_subscriber, typeof(TaskListUpdated));
        _actorSystem.EventStream.Subscribe(_subscriber, typeof(TaskTimerTick));
    }

    protected override void OnNavigatingFrom()
    {
        // Unsubscribe and stop subscriber
        if (_subscriber != null)
        {
            _actorSystem.EventStream.Unsubscribe(_subscriber);
            _actorSystem.Stop(_subscriber);
            _subscriber = null;
        }
    }

    internal void OnTaskListUpdated(TaskListUpdated evt)
    {
        // Find our task in the updated list
        if (_currentTask != null)
        {
            var updated = evt.Tasks.FirstOrDefault(t => t.Id == _currentTask.Id);
            if (updated != null)
            {
                _currentTask = updated;
                Send(new TaskDetailCommand.UpdateTask(updated));
            }
        }
    }

    internal void OnTimerTick(TaskTimerTick evt)
    {
        // Update timer display if it's our task
        if (_currentTask != null && _currentTask.Id == evt.TaskId)
        {
            _currentTask = _currentTask with { ElapsedTime = evt.Elapsed };
            Send(new TaskDetailCommand.UpdateTask(_currentTask));
        }
    }

    protected override void HandleUIEvent(TaskDetailUIEvent evt)
    {
        if (_currentTask == null)
        {
            Send(new TaskDetailCommand.ShowStatus("Error: No task loaded"));
            return;
        }

        switch (evt)
        {
            case TaskDetailUIEvent.ToggleTimerRequested:
                if (_currentTask.IsTimerRunning)
                    _taskManager.Tell(new StopTimer(_currentTask.Id));
                else
                    _taskManager.Tell(new StartTimer(_currentTask.Id));
                break;

            case TaskDetailUIEvent.ToggleCompletionRequested:
                _taskManager.Tell(new ToggleTask(_currentTask.Id));
                break;

            case TaskDetailUIEvent.ChangePriorityRequested(var priority):
                _taskManager.Tell(new SetPriority(_currentTask.Id, priority));
                break;

            case TaskDetailUIEvent.BackRequested:
                Navigate("tasks");
                break;
        }
    }

    /// <summary>
    /// Actor that receives events from EventStream and forwards to handler.
    /// </summary>
    private sealed class EventSubscriber : UntypedActor
    {
        private readonly TaskDetailHandler _handler;

        public EventSubscriber(TaskDetailHandler handler) => _handler = handler;

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case TaskListUpdated evt:
                    _handler.OnTaskListUpdated(evt);
                    break;
                case TaskTimerTick evt:
                    _handler.OnTimerTick(evt);
                    break;
            }
        }
    }
}

using Akka.Actor;
using Akka.Hosting;
using Termina.Demo.Akka.Actors;
using Termina.Pages;

namespace Termina.Demo.Akka.Pages;

/// <summary>
/// Handler for the task list page.
/// Injects actor reference via DI and manages communication with the actor.
/// </summary>
public sealed class TaskListHandler
    : PageHandler<TaskListPage, TaskListUIEvent, TaskListCommand>
{
    private readonly IActorRef _taskManager;
    private readonly ActorSystem _actorSystem;
    private IActorRef? _subscriber;

    public TaskListHandler(IRequiredActor<TaskManagerActor> taskManagerProvider, ActorSystem actorSystem)
    {
        _taskManager = taskManagerProvider.ActorRef;
        _actorSystem = actorSystem;
    }

    protected override void OnNavigatedTo()
    {
        // Create a subscriber actor to receive events from EventStream
        _subscriber = _actorSystem.ActorOf(Props.Create(() => new EventSubscriber(this)));

        // Subscribe to events
        _actorSystem.EventStream.Subscribe(_subscriber, typeof(TaskListUpdated));
        _actorSystem.EventStream.Subscribe(_subscriber, typeof(TaskTimerTick));
        _actorSystem.EventStream.Subscribe(_subscriber, typeof(TaskDetailRequested));

        // Request current state from actor
        _taskManager.Tell(new GetCurrentTasks());
    }

    protected override void OnNavigatingFrom()
    {
        // Unsubscribe and stop the subscriber actor
        if (_subscriber != null)
        {
            _actorSystem.EventStream.Unsubscribe(_subscriber);
            _actorSystem.Stop(_subscriber);
            _subscriber = null;
        }
    }

    internal void OnTaskListUpdated(TaskListUpdated evt)
    {
        Send(new TaskListCommand.UpdateTaskList(evt.Tasks, evt.Stats));
    }

    internal void OnTimerTick(TaskTimerTick evt)
    {
        Send(new TaskListCommand.UpdateTaskTimer(evt.TaskId, evt.Elapsed));
    }

    internal void OnDetailRequested(TaskDetailRequested evt)
    {
        // Navigate to detail page with task data
        // Store task data that will be passed to detail handler
        TaskDetailHandler.SetPendingTask(evt.Task);
        Navigate("task-detail");
    }

    protected override void HandleUIEvent(TaskListUIEvent evt)
    {
        switch (evt)
        {
            case TaskListUIEvent.AddTaskRequested(var description, var priority):
                _taskManager.Tell(new AddTask(description, priority));
                break;

            case TaskListUIEvent.RemoveTaskRequested(var taskId):
                _taskManager.Tell(new RemoveTask(taskId));
                break;

            case TaskListUIEvent.ToggleTaskRequested(var taskId):
                _taskManager.Tell(new ToggleTask(taskId));
                break;

            case TaskListUIEvent.ChangePriorityRequested(var taskId, var priority):
                _taskManager.Tell(new SetPriority(taskId, priority));
                break;

            case TaskListUIEvent.StartTimerRequested(var taskId):
                _taskManager.Tell(new StartTimer(taskId));
                break;

            case TaskListUIEvent.StopTimerRequested(var taskId):
                _taskManager.Tell(new StopTimer(taskId));
                break;

            case TaskListUIEvent.ViewDetailRequested(var taskId):
                _taskManager.Tell(new ViewTaskDetail(taskId));
                break;

            case TaskListUIEvent.BackRequested:
                Shutdown();
                break;
        }
    }

    /// <summary>
    /// Actor that receives events from EventStream and forwards to handler.
    /// </summary>
    private sealed class EventSubscriber : UntypedActor
    {
        private readonly TaskListHandler _handler;

        public EventSubscriber(TaskListHandler handler) => _handler = handler;

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
                case TaskDetailRequested evt:
                    _handler.OnDetailRequested(evt);
                    break;
            }
        }
    }
}

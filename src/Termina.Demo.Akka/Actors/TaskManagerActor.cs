using Akka.Actor;
using Termina.Pages;

namespace Termina.Demo.Akka.Actors;

/// <summary>
/// Actor that manages a list of tasks with optional timers.
/// Publishes state changes to the UI via IApplicationBus.
/// </summary>
public sealed class TaskManagerActor : ReceiveActor, IWithTimers
{
    private readonly IApplicationBus _bus;
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public ITimerScheduler Timers { get; set; } = null!;

    public TaskManagerActor(IApplicationBus bus)
    {
        _bus = bus;

        Receive<AddTask>(cmd =>
        {
            var task = new TaskItem(
                Id: _nextId++,
                Description: cmd.Description,
                IsCompleted: false,
                IsTimerRunning: false,
                ElapsedTime: TimeSpan.Zero,
                StartedAt: null);

            _tasks.Add(task);
            PublishTaskList();
        });

        Receive<RemoveTask>(cmd =>
        {
            var index = _tasks.FindIndex(t => t.Id == cmd.Id);
            if (index >= 0)
            {
                var task = _tasks[index];
                if (task.IsTimerRunning)
                {
                    Timers.Cancel($"timer-{task.Id}");
                }
                _tasks.RemoveAt(index);
                PublishTaskList();
            }
        });

        Receive<ToggleTask>(cmd =>
        {
            var index = _tasks.FindIndex(t => t.Id == cmd.Id);
            if (index >= 0)
            {
                var task = _tasks[index];
                _tasks[index] = task with { IsCompleted = !task.IsCompleted };
                PublishTaskList();
            }
        });

        Receive<StartTimer>(cmd =>
        {
            var index = _tasks.FindIndex(t => t.Id == cmd.Id);
            if (index >= 0)
            {
                var task = _tasks[index];
                if (!task.IsTimerRunning)
                {
                    _tasks[index] = task with
                    {
                        IsTimerRunning = true,
                        StartedAt = DateTime.UtcNow
                    };

                    // Schedule periodic ticks for this task
                    Timers.StartPeriodicTimer(
                        $"timer-{task.Id}",
                        new Tick(),
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromMilliseconds(100));

                    PublishTaskList();
                }
            }
        });

        Receive<StopTimer>(cmd =>
        {
            var index = _tasks.FindIndex(t => t.Id == cmd.Id);
            if (index >= 0)
            {
                var task = _tasks[index];
                if (task.IsTimerRunning)
                {
                    Timers.Cancel($"timer-{task.Id}");

                    var elapsed = task.ElapsedTime;
                    if (task.StartedAt.HasValue)
                    {
                        elapsed += DateTime.UtcNow - task.StartedAt.Value;
                    }

                    _tasks[index] = task with
                    {
                        IsTimerRunning = false,
                        ElapsedTime = elapsed,
                        StartedAt = null
                    };
                    PublishTaskList();
                }
            }
        });

        Receive<Tick>(_ =>
        {
            // Update elapsed time for all running timers and publish
            var hasRunningTimers = false;
            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                if (task.IsTimerRunning && task.StartedAt.HasValue)
                {
                    hasRunningTimers = true;
                    var currentElapsed = task.ElapsedTime + (DateTime.UtcNow - task.StartedAt.Value);
                    _bus.Publish(new TaskTimerTick(task.Id, currentElapsed));
                }
            }

            // If we have running timers, also publish the full list periodically
            // to keep UI in sync
            if (hasRunningTimers)
            {
                PublishTaskList();
            }
        });
    }

    private void PublishTaskList()
    {
        // Calculate current elapsed times for display
        var tasksWithCurrentElapsed = _tasks.Select(t =>
        {
            if (t.IsTimerRunning && t.StartedAt.HasValue)
            {
                var currentElapsed = t.ElapsedTime + (DateTime.UtcNow - t.StartedAt.Value);
                return t with { ElapsedTime = currentElapsed };
            }
            return t;
        }).ToList();

        _bus.Publish(new TaskListUpdated(tasksWithCurrentElapsed));
    }

    protected override void PreStart()
    {
        // Publish initial empty state
        PublishTaskList();
        base.PreStart();
    }
}

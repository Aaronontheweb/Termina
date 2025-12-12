using Akka.Actor;
using Akka.Event;

namespace Termina.Demo.Akka.Actors;

/// <summary>
/// Actor that manages a list of tasks with optional timers.
/// Publishes state changes via Akka EventStream.
/// Pre-stocks demo tasks on startup to showcase functionality.
/// </summary>
public sealed class TaskManagerActor : ReceiveActor, IWithTimers
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public ITimerScheduler Timers { get; set; } = null!;

    public TaskManagerActor()
    {

        Receive<AddTask>(cmd =>
        {
            var task = new TaskItem(
                Id: _nextId++,
                Description: cmd.Description,
                Priority: cmd.Priority,
                IsCompleted: false,
                IsTimerRunning: false,
                ElapsedTime: TimeSpan.Zero,
                EstimatedDuration: GetEstimatedDuration(cmd.Priority),
                StartedAt: null,
                CreatedAt: DateTime.UtcNow);

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

        Receive<SetPriority>(cmd =>
        {
            var index = _tasks.FindIndex(t => t.Id == cmd.Id);
            if (index >= 0)
            {
                var task = _tasks[index];
                _tasks[index] = task with
                {
                    Priority = cmd.Priority,
                    EstimatedDuration = GetEstimatedDuration(cmd.Priority)
                };
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

        Receive<ViewTaskDetail>(cmd =>
        {
            var task = _tasks.FirstOrDefault(t => t.Id == cmd.Id);
            if (task != null)
            {
                // Calculate current elapsed for running timers
                if (task.IsTimerRunning && task.StartedAt.HasValue)
                {
                    task = task with
                    {
                        ElapsedTime = task.ElapsedTime + (DateTime.UtcNow - task.StartedAt.Value)
                    };
                }
                Context.System.EventStream.Publish(new TaskDetailRequested(task));
            }
        });

        Receive<GetCurrentTasks>(_ =>
        {
            // Return current state - used when handler first subscribes
            PublishTaskList();
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
                    Context.System.EventStream.Publish(new TaskTimerTick(task.Id, currentElapsed));
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

    private static TimeSpan? GetEstimatedDuration(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => TimeSpan.FromMinutes(15),
        TaskPriority.High => TimeSpan.FromMinutes(30),
        TaskPriority.Medium => TimeSpan.FromHours(1),
        TaskPriority.Low => TimeSpan.FromHours(2),
        _ => null
    };

    private TaskStatistics ComputeStatistics()
    {
        var totalTimeTracked = TimeSpan.Zero;
        var activeTimers = 0;
        var highPriorityPending = 0;
        var criticalPending = 0;
        var completed = 0;

        foreach (var task in _tasks)
        {
            var elapsed = task.ElapsedTime;
            if (task.IsTimerRunning && task.StartedAt.HasValue)
            {
                elapsed += DateTime.UtcNow - task.StartedAt.Value;
                activeTimers++;
            }
            totalTimeTracked += elapsed;

            if (task.IsCompleted)
            {
                completed++;
            }
            else
            {
                if (task.Priority == TaskPriority.High) highPriorityPending++;
                if (task.Priority == TaskPriority.Critical) criticalPending++;
            }
        }

        return new TaskStatistics(
            TotalTasks: _tasks.Count,
            CompletedTasks: completed,
            ActiveTimers: activeTimers,
            TotalTimeTracked: totalTimeTracked,
            HighPriorityPending: highPriorityPending,
            CriticalPending: criticalPending);
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

        Context.System.EventStream.Publish(new TaskListUpdated(tasksWithCurrentElapsed, ComputeStatistics()));
    }

    protected override void PreStart()
    {
        // Pre-stock with demo tasks to showcase functionality
        var demoTasks = new[]
        {
            (Desc: "Review pull request #42", Priority: TaskPriority.Critical, Running: true, Elapsed: TimeSpan.FromMinutes(3)),
            (Desc: "Fix production bug", Priority: TaskPriority.High, Running: true, Elapsed: TimeSpan.FromMinutes(12)),
            (Desc: "Write unit tests", Priority: TaskPriority.Medium, Running: false, Elapsed: TimeSpan.FromMinutes(25)),
            (Desc: "Update documentation", Priority: TaskPriority.Low, Running: false, Elapsed: TimeSpan.Zero),
            (Desc: "Refactor legacy code", Priority: TaskPriority.Medium, Running: false, Elapsed: TimeSpan.FromMinutes(45)),
            (Desc: "Deploy to staging", Priority: TaskPriority.High, Running: false, Elapsed: TimeSpan.Zero),
        };

        foreach (var demo in demoTasks)
        {
            var task = new TaskItem(
                Id: _nextId++,
                Description: demo.Desc,
                Priority: demo.Priority,
                IsCompleted: false,
                IsTimerRunning: false,
                ElapsedTime: demo.Elapsed,
                EstimatedDuration: GetEstimatedDuration(demo.Priority),
                StartedAt: null,
                CreatedAt: DateTime.UtcNow.AddMinutes(-Random.Shared.Next(5, 60)));

            _tasks.Add(task);

            // Start timers for tasks that should be running
            if (demo.Running)
            {
                var index = _tasks.Count - 1;
                _tasks[index] = task with
                {
                    IsTimerRunning = true,
                    StartedAt = DateTime.UtcNow
                };

                Timers.StartPeriodicTimer(
                    $"timer-{task.Id}",
                    new Tick(),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(100));
            }
        }

        // Publish initial state
        PublishTaskList();
        base.PreStart();
    }
}

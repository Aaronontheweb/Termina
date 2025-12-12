namespace Termina.Demo.Akka.Actors;

/// <summary>
/// Task priority levels with associated colors.
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

// Commands from UI to Actor
public sealed record AddTask(string Description, TaskPriority Priority = TaskPriority.Medium);
public sealed record RemoveTask(int Id);
public sealed record ToggleTask(int Id);
public sealed record SetPriority(int Id, TaskPriority Priority);
public sealed record StartTimer(int Id);
public sealed record StopTimer(int Id);
public sealed record ViewTaskDetail(int Id);

// Request for current state (query pattern)
public sealed record GetCurrentTasks;

// Internal actor messages
internal sealed record Tick;

// Events published by the actor to Akka EventStream (subscribed by handlers)
public sealed record TaskListUpdated(IReadOnlyList<TaskItem> Tasks, TaskStatistics Stats);
public sealed record TaskTimerTick(int TaskId, TimeSpan Elapsed);
public sealed record TaskDetailRequested(TaskItem Task);

// Domain model
public sealed record TaskItem(
    int Id,
    string Description,
    TaskPriority Priority,
    bool IsCompleted,
    bool IsTimerRunning,
    TimeSpan ElapsedTime,
    TimeSpan? EstimatedDuration,
    DateTime? StartedAt,
    DateTime CreatedAt);

/// <summary>
/// Aggregate statistics computed by the actor.
/// </summary>
public sealed record TaskStatistics(
    int TotalTasks,
    int CompletedTasks,
    int ActiveTimers,
    TimeSpan TotalTimeTracked,
    int HighPriorityPending,
    int CriticalPending);

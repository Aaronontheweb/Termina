namespace Termina.Demo.Akka.Actors;

// Commands from UI to Actor
public sealed record AddTask(string Description);
public sealed record RemoveTask(int Id);
public sealed record ToggleTask(int Id);
public sealed record StartTimer(int Id);
public sealed record StopTimer(int Id);

// Internal actor messages
internal sealed record Tick;

// Model events from Actor to UI (implement IModelEvent)
public sealed record TaskListUpdated(IReadOnlyList<TaskItem> Tasks) : Termina.Events.IModelEvent;
public sealed record TaskTimerTick(int TaskId, TimeSpan Elapsed) : Termina.Events.IModelEvent;

// Domain model
public sealed record TaskItem(
    int Id,
    string Description,
    bool IsCompleted,
    bool IsTimerRunning,
    TimeSpan ElapsedTime,
    DateTime? StartedAt);

using System.Threading.Channels;
using Termina.Events;
using Termina.Pages;

namespace Termina.Tests;

/// <summary>
/// Tests for the ApplicationBus implementation.
/// </summary>
public class ApplicationBusTests
{
    private record TestModelEvent(string Message) : IModelEvent;
    private record OtherModelEvent(int Value) : IModelEvent;

    [Fact]
    public void Subscribe_ShouldReceivePublishedEvents()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<object>();
        var bus = CreateApplicationBus(channel);
        var received = new List<TestModelEvent>();

        using var subscription = bus.Subscribe<TestModelEvent>(evt => received.Add(evt));

        // Act
        bus.Publish(new TestModelEvent("Hello"));
        bus.Publish(new TestModelEvent("World"));

        // Assert
        Assert.Equal(2, received.Count);
        Assert.Equal("Hello", received[0].Message);
        Assert.Equal("World", received[1].Message);
    }

    [Fact]
    public void Subscribe_ShouldOnlyReceiveEventsOfCorrectType()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<object>();
        var bus = CreateApplicationBus(channel);
        var testEvents = new List<TestModelEvent>();
        var otherEvents = new List<OtherModelEvent>();

        using var testSub = bus.Subscribe<TestModelEvent>(evt => testEvents.Add(evt));
        using var otherSub = bus.Subscribe<OtherModelEvent>(evt => otherEvents.Add(evt));

        // Act
        bus.Publish(new TestModelEvent("Test"));
        bus.Publish(new OtherModelEvent(42));
        bus.Publish(new TestModelEvent("Test2"));

        // Assert
        Assert.Equal(2, testEvents.Count);
        Assert.Single(otherEvents);
        Assert.Equal(42, otherEvents[0].Value);
    }

    [Fact]
    public void Unsubscribe_ShouldStopReceivingEvents()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<object>();
        var bus = CreateApplicationBus(channel);
        var received = new List<TestModelEvent>();

        var subscription = bus.Subscribe<TestModelEvent>(evt => received.Add(evt));

        // Act
        bus.Publish(new TestModelEvent("Before"));
        subscription.Dispose();
        bus.Publish(new TestModelEvent("After"));

        // Assert
        Assert.Single(received);
        Assert.Equal("Before", received[0].Message);
    }

    [Fact]
    public void Publish_ShouldWriteToChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<object>();
        var bus = CreateApplicationBus(channel);

        // Act
        bus.Publish(new TestModelEvent("Test"));

        // Assert
        Assert.True(channel.Reader.TryRead(out var evt));
        var testEvent = Assert.IsType<TestModelEvent>(evt);
        Assert.Equal("Test", testEvent.Message);
    }

    [Fact]
    public void DeadLetter_ShouldRaiseEventWhenCalled()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<object>();
        var bus = CreateApplicationBus(channel);
        object? deadLetterEvent = null;
        string? deadLetterReason = null;

        bus.DeadLetter += (evt, reason) =>
        {
            deadLetterEvent = evt;
            deadLetterReason = reason;
        };

        // Act - use reflection to call internal RaiseDeadLetter
        var testEvent = new TestModelEvent("Unhandled");
        var raiseMethod = bus.GetType().GetMethod("RaiseDeadLetter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(raiseMethod);
        raiseMethod.Invoke(bus, [testEvent, "No handler registered"]);

        // Assert
        Assert.Equal(testEvent, deadLetterEvent);
        Assert.Equal("No handler registered", deadLetterReason);
    }

    private static IApplicationBus CreateApplicationBus(Channel<object> channel)
    {
        // Use reflection to create internal ApplicationBus
        var busType = typeof(IApplicationBus).Assembly.GetType("Termina.ApplicationBus");
        Assert.NotNull(busType);

        var instance = Activator.CreateInstance(busType, channel);
        Assert.NotNull(instance);

        return (IApplicationBus)instance;
    }
}

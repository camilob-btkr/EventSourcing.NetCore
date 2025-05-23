using FluentAssertions;
using Marten.Integration.Tests.TestsInfrastructure;
using Xunit;

namespace Marten.Integration.Tests.EventStore.Stream;

public class StreamLoadingFromExactState(MartenFixture fixture): MartenTest(fixture.PostgreSqlContainer)
{
    public record IssueCreated(
        Guid IssueId,
        string Description
    );

    public record IssueUpdated(
        Guid IssueId,
        string Description
    );

    [Fact(Skip = "Skipping - AppVeyor for some reason doesn't like it -_-")]
    public async Task GivenSetOfEvents_WithFetchEventsFromDifferentTimes_ThenProperSetsAreLoaded()
    {
        //Given
        var streamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        //When
        var beforeCreateTimestamp = DateTime.UtcNow;

        EventStore.Append(streamId, new IssueCreated(taskId, "Initial Name"));
        await Session.SaveChangesAsync();
        var createTimestamp = DateTime.UtcNow;

        EventStore.Append(streamId, new IssueUpdated(taskId, "Updated name"));
        await Session.SaveChangesAsync();
        var firstUpdateTimestamp = DateTime.UtcNow;

        EventStore.Append(streamId, new IssueUpdated(taskId, "Updated again name"),
            new IssueUpdated(taskId, "Updated again and again name"));
        await Session.SaveChangesAsync();
        var secondUpdateTimestamp = DateTime.UtcNow;

        //Then
        var events = await EventStore.FetchStreamAsync(streamId, timestamp: beforeCreateTimestamp);
        events.Count.Should().Be(0);

        events = await EventStore.FetchStreamAsync(streamId, timestamp: createTimestamp);
        events.Count.Should().Be(1);

        events = await EventStore.FetchStreamAsync(streamId, timestamp: firstUpdateTimestamp);
        events.Count.Should().Be(2);

        events = await EventStore.FetchStreamAsync(streamId, timestamp: secondUpdateTimestamp);
        events.Count.Should().Be(4);
    }

    [Fact]
    public async Task GivenSetOfEvents_WithFetchEventsFromDifferentVersionNumber_ThenProperSetsAreLoaded()
    {
        //Given
        var streamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        //When
        EventStore.Append(streamId, new IssueCreated(taskId, "Initial Name"));
        await Session.SaveChangesAsync();

        EventStore.Append(streamId, new IssueUpdated(taskId, "Updated name"));
        await Session.SaveChangesAsync();

        EventStore.Append(streamId, new IssueUpdated(taskId, "Updated again name"),
            new IssueUpdated(taskId, "Updated again and again name"));
        await Session.SaveChangesAsync();

        //Then
        //version after create
        var events = await EventStore.FetchStreamAsync(streamId, 1);
        events.Count.Should().Be(1);

        //version after first update
        events = await EventStore.FetchStreamAsync(streamId, 2);
        events.Count.Should().Be(2);

        //even though 3 and 4 updates were append at the same time version is incremented for both of them
        events = await EventStore.FetchStreamAsync(streamId, 3);
        events.Count.Should().Be(3);

        events = await EventStore.FetchStreamAsync(streamId, 4);
        events.Count.Should().Be(4);

        //fetching with version equal to 0 returns the most recent state
        events = await EventStore.FetchStreamAsync(streamId, 0);
        events.Count.Should().Be(4);

        //providing bigger version than current doesn't throws exception - returns most recent state
        events = await EventStore.FetchStreamAsync(streamId, 100);
        events.Count.Should().Be(4);
    }
}

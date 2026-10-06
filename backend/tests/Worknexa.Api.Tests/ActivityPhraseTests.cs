using Worknexa.Application.Platform;

namespace Worknexa.Api.Tests;

public sealed class ActivityPhraseTests
{
    [Fact]
    public void Phrases_MatchTheTimelineSentences()
    {
        Assert.Equal("Worknexa Admin assigned API Documentation to John.", ActivityPhrases.Assigned("Worknexa Admin", "API Documentation", "John"));
        Assert.Equal("John completed Authentication API.", ActivityPhrases.Completed("John", "Authentication API"));
        Assert.Equal("TravelDesk moved to In Progress.", ActivityPhrases.Moved("TravelDesk", ActivityPhrases.Label("InProgress")));
        Assert.Equal("On Hold", ActivityPhrases.Label("OnHold"));
    }
}

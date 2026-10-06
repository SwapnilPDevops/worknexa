using Worknexa.Application.Assistant;
using Worknexa.Application.Work;

namespace Worknexa.Api.Tests;

public sealed class AssistantResolveTests
{
    private static readonly DateOnly Tuesday = new(2026, 10, 6);

    [Theory]
    [InlineData("Friday", "2026-10-09")]
    [InlineData("by Friday", "2026-10-09")]
    [InlineData("on tuesday", "2026-10-06")]
    [InlineData("Monday", "2026-10-12")]
    [InlineData("next Friday", "2026-10-16")]
    [InlineData("next week", "2026-10-12")]
    [InlineData("today", "2026-10-06")]
    [InlineData("tomorrow", "2026-10-07")]
    [InlineData("in 3 days", "2026-10-09")]
    [InlineData("in 2 weeks", "2026-10-20")]
    [InlineData("end of week", "2026-10-09")]
    [InlineData("2026-11-02", "2026-11-02")]
    [InlineData("by 2026-10-30", "2026-10-30")]
    public void Dates_ResolveEverydayWords(string text, string expected)
    {
        Assert.Equal(DateOnly.Parse(expected), AssistantDates.Resolve(text, Tuesday));
    }

    [Theory]
    [InlineData("")]
    [InlineData("someday")]
    [InlineData("5")]
    public void Dates_ReturnNothingForUnreadableText(string text)
    {
        Assert.Null(AssistantDates.Resolve(text, Tuesday));
    }

    [Theory]
    [InlineData("15:00", 15, 0)]
    [InlineData("3pm", 15, 0)]
    [InlineData("9:30 am", 9, 30)]
    [InlineData("9", 9, 0)]
    public void Times_ParseCommonForms(string text, int hour, int minute)
    {
        Assert.Equal(new TimeOnly(hour, minute), AssistantDates.ResolveTime(text));
    }

    [Fact]
    public void Names_PreferExactThenUniquePrefix()
    {
        NamedRef[] people = [new(Guid.NewGuid(), "John Smith"), new(Guid.NewGuid(), "Johanna Lee"), new(Guid.NewGuid(), "Priya Nair")];

        Assert.Equal("John Smith", NameMatch.Find(people, p => p.Id, p => p.Name, "john smith").Item?.Name);
        Assert.Equal("Priya Nair", NameMatch.Find(people, p => p.Id, p => p.Name, "priya").Item?.Name);

        var ambiguous = NameMatch.Find(people, p => p.Id, p => p.Name, "Joh");
        Assert.True(ambiguous.Ambiguous);
        Assert.Equal(2, ambiguous.Candidates.Count);

        Assert.Empty(NameMatch.Find(people, p => p.Id, p => p.Name, "Zed").Candidates);
    }

    [Fact]
    public void Names_FindProjectsMentionedInATitle()
    {
        NamedRef[] projects = [new(Guid.NewGuid(), "TravelDesk"), new(Guid.NewGuid(), "Travel"), new(Guid.NewGuid(), "Launch")];

        var mentioned = NameMatch.MentionedIn(projects, p => p.Name, "TravelDesk API documentation");

        Assert.Equal("TravelDesk", Assert.Single(mentioned).Name);
        Assert.Empty(NameMatch.MentionedIn(projects, p => p.Name, "Relaunch the site"));
    }
}

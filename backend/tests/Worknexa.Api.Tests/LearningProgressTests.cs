using Worknexa.Application.Learning;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class LearningProgressTests
{
    [Fact]
    public void Rollup_AveragesChildrenAndKeepsLeaves()
    {
        var root = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var topics = new List<LearningProgressInput>
        {
            new(root, null, 5),
            new(left, root, 40),
            new(right, root, 80)
        };

        var rolled = LearningProgress.Rollup(topics);

        Assert.Equal(40, rolled[left]);
        Assert.Equal(80, rolled[right]);
        Assert.Equal(60, rolled[root]);
        Assert.Equal(60, LearningProgress.Overall(topics, rolled));
    }

    [Fact]
    public void Rollup_ClampsStoredProgressAndSuggestsAStartingPoint()
    {
        var leaf = Guid.NewGuid();
        var rolled = LearningProgress.Rollup([new LearningProgressInput(leaf, null, 140)]);

        Assert.Equal(100, rolled[leaf]);
        Assert.Equal(0, LearningProgress.Suggested(LearningStatus.NotStarted));
        Assert.Equal(25, LearningProgress.Suggested(LearningStatus.Learning));
        Assert.Equal(60, LearningProgress.Suggested(LearningStatus.Practicing));
        Assert.Equal(100, LearningProgress.Suggested(LearningStatus.Completed));
    }
}

using Worknexa.Domain.Enums;

namespace Worknexa.Application.Learning;

public sealed record LearningProgressInput(Guid Id, Guid? ParentId, int Progress);

public static class LearningProgress
{
    public static IReadOnlyDictionary<Guid, int> Rollup(IReadOnlyList<LearningProgressInput> topics)
    {
        var byId = topics.ToDictionary(topic => topic.Id);
        var children = new Dictionary<Guid, List<Guid>>();
        foreach (var topic in topics)
        {
            if (topic.ParentId is not Guid parentId || !byId.ContainsKey(parentId))
            {
                continue;
            }

            if (!children.TryGetValue(parentId, out var list))
            {
                list = [];
                children[parentId] = list;
            }

            list.Add(topic.Id);
        }

        var cache = new Dictionary<Guid, int>();
        int Walk(Guid id)
        {
            if (cache.TryGetValue(id, out var stored))
            {
                return stored;
            }

            var kids = children.TryGetValue(id, out var list) ? list : [];
            var value = kids.Count == 0
                ? Math.Clamp(byId[id].Progress, 0, 100)
                : (int)Math.Round(kids.Average(kid => (double)Walk(kid)), MidpointRounding.AwayFromZero);
            cache[id] = value;
            return value;
        }

        foreach (var topic in topics)
        {
            Walk(topic.Id);
        }

        return cache;
    }

    public static int Overall(IReadOnlyList<LearningProgressInput> topics, IReadOnlyDictionary<Guid, int> rolled)
    {
        var roots = topics.Where(topic => topic.ParentId is null || !rolled.ContainsKey(topic.ParentId.Value)).Select(topic => rolled[topic.Id]).ToList();
        return roots.Count == 0 ? 0 : (int)Math.Round(roots.Average(), MidpointRounding.AwayFromZero);
    }

    public static int Suggested(LearningStatus status)
    {
        return status switch
        {
            LearningStatus.Learning => 25,
            LearningStatus.Practicing => 60,
            LearningStatus.Completed => 100,
            _ => 0
        };
    }
}

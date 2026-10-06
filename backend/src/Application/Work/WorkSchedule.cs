using Worknexa.Application.Dashboard;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Work;

public enum DueBucket
{
    Overdue,
    Today,
    Tomorrow,
    ThisWeek,
    Upcoming,
    Completed
}

public static class DueSchedule
{
    public static DueBucket Bucket(DateTimeOffset? dueAt, bool completed, MyDayWindow window)
    {
        if (completed)
        {
            return DueBucket.Completed;
        }

        if (dueAt is null)
        {
            return DueBucket.Today;
        }

        if (dueAt < window.DayStart)
        {
            return DueBucket.Overdue;
        }

        if (dueAt < window.DayEnd)
        {
            return DueBucket.Today;
        }

        if (dueAt < window.DayEnd.AddDays(1))
        {
            return DueBucket.Tomorrow;
        }

        var untilSunday = ((int)DayOfWeek.Sunday - (int)window.LocalDate.DayOfWeek + 7) % 7;
        if (dueAt < window.DayEnd.AddDays(untilSunday))
        {
            return DueBucket.ThisWeek;
        }

        return DueBucket.Upcoming;
    }

    public static bool CanAdvance(Recurrence recurrence, int? interval = null)
    {
        return recurrence switch
        {
            Recurrence.None => false,
            Recurrence.Custom => interval is > 0 and <= 366,
            _ => true
        };
    }

    public static DateTimeOffset Next(DateTimeOffset dueAt, Recurrence recurrence, int? interval = null)
    {
        return Advance(dueAt, recurrence, interval);
    }

    public static DateTimeOffset Advance(DateTimeOffset value, Recurrence recurrence, int? interval = null)
    {
        return recurrence switch
        {
            Recurrence.Daily => value.AddDays(1),
            Recurrence.Weekly => value.AddDays(7),
            Recurrence.Monthly => value.AddMonths(1),
            Recurrence.Custom when interval is > 0 and <= 366 => value.AddDays(interval.Value),
            _ => value
        };
    }

    public static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> Occurrences(
        DateTimeOffset start,
        DateTimeOffset end,
        Recurrence recurrence,
        int? interval,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd)
    {
        var duration = end >= start ? end - start : TimeSpan.Zero;
        var results = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        var cursor = start;
        if (CanAdvance(recurrence, interval) && cursor + duration <= rangeStart)
        {
            cursor = Jump(cursor, rangeStart, duration, recurrence, interval);
        }

        for (var guard = 0; guard < 400 && cursor < rangeEnd; guard++)
        {
            var occurrenceEnd = cursor + duration;
            if (occurrenceEnd > rangeStart)
            {
                results.Add((cursor, occurrenceEnd));
            }

            if (!CanAdvance(recurrence, interval))
            {
                break;
            }

            var next = Advance(cursor, recurrence, interval);
            if (next <= cursor)
            {
                break;
            }

            cursor = next;
        }

        return results;
    }

    private static DateTimeOffset Jump(
        DateTimeOffset cursor,
        DateTimeOffset rangeStart,
        TimeSpan duration,
        Recurrence recurrence,
        int? interval)
    {
        var target = rangeStart - duration;
        if (target <= cursor)
        {
            return cursor;
        }

        var steps = recurrence switch
        {
            Recurrence.Daily => (int)Math.Floor((target - cursor).TotalDays),
            Recurrence.Weekly => (int)Math.Floor((target - cursor).TotalDays / 7),
            Recurrence.Custom when interval is > 0 => (int)Math.Floor((target - cursor).TotalDays / interval.Value),
            Recurrence.Monthly => ((target.Year - cursor.Year) * 12) + target.Month - cursor.Month - 1,
            _ => 0
        };

        if (steps <= 0)
        {
            return cursor;
        }

        return recurrence switch
        {
            Recurrence.Daily => cursor.AddDays(steps),
            Recurrence.Weekly => cursor.AddDays(steps * 7),
            Recurrence.Custom => cursor.AddDays(steps * interval!.Value),
            Recurrence.Monthly => cursor.AddMonths(steps),
            _ => cursor
        };
    }
}

public static class TagNames
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? names)
    {
        if (names is null)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            result.Add(trimmed.Length <= 100 ? trimmed : trimmed[..100]);
            if (result.Count == 12)
            {
                break;
            }
        }

        return result;
    }
}

public static class WorkGraphs
{
    public static bool CreatesCycle(Guid originId, Guid nextId, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> links)
    {
        if (originId == nextId)
        {
            return true;
        }

        var seen = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(nextId);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!seen.Add(current))
            {
                continue;
            }

            if (current == originId)
            {
                return true;
            }

            if (links.TryGetValue(current, out var next))
            {
                foreach (var id in next)
                {
                    pending.Push(id);
                }
            }
        }

        return false;
    }
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace Worknexa.Application.Assistant;

public sealed record Match<T>(T? Item, IReadOnlyList<T> Candidates)
    where T : class
{
    public bool Ambiguous => Item is null && Candidates.Count > 1;
}

public static partial class NameMatch
{
    /// <summary>
    /// Exact id or name first, then names where every word of the query starts a word of the name
    /// ("john" matches "John Smith"). Several equal matches are reported back instead of guessing.
    /// </summary>
    public static Match<T> Find<T>(IEnumerable<T> items, Func<T, Guid> id, Func<T, string> name, string query, Func<T, string?>? alias = null)
        where T : class
    {
        var list = items as IReadOnlyList<T> ?? items.ToArray();
        var text = query.Trim();
        if (Guid.TryParse(text, out var parsed))
        {
            var byId = list.FirstOrDefault(item => id(item) == parsed);
            return new Match<T>(byId, byId is null ? [] : [byId]);
        }

        var exact = list.Where(item =>
            string.Equals(name(item).Trim(), text, StringComparison.OrdinalIgnoreCase)
            || (alias is not null && string.Equals(alias(item)?.Trim(), text, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (exact.Length > 0)
        {
            return new Match<T>(exact.Length == 1 ? exact[0] : null, exact);
        }

        var wanted = Words(text);
        if (wanted.Length == 0)
        {
            return new Match<T>(null, []);
        }

        var partial = list.Where(item =>
        {
            var words = Words(name(item));
            return wanted.All(part => words.Any(word => word.StartsWith(part, StringComparison.OrdinalIgnoreCase)));
        }).ToArray();
        return new Match<T>(partial.Length == 1 ? partial[0] : null, partial);
    }

    /// <summary>Items whose whole name appears as words inside the text, longest name first.</summary>
    public static IReadOnlyList<T> MentionedIn<T>(IEnumerable<T> items, Func<T, string> name, string text)
    {
        var padded = $" {string.Join(' ', Words(text))} ";
        var hits = items
            .Where(item => Words(name(item)) is { Length: > 0 } words && padded.Contains($" {string.Join(' ', words)} ", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var longest = hits.Select(item => name(item).Length).DefaultIfEmpty(0).Max();
        return hits.Where(item => name(item).Length == longest).ToArray();
    }

    private static string[] Words(string value) => WordPattern().Matches(value).Select(match => match.Value).ToArray();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordPattern();
}

public static partial class AssistantDates
{
    private static readonly string[] IsoFormats = ["yyyy-MM-dd", "yyyy-M-d", "d MMM yyyy", "d MMMM yyyy", "MMM d yyyy", "MMMM d yyyy"];

    /// <summary>
    /// ISO dates and everyday words: today, tomorrow, a weekday (the next one on or after today),
    /// "next Friday" (in the following week), "in 3 days", "next week".
    /// </summary>
    public static DateOnly? Resolve(string? text, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var value = LeadingWords().Replace(text.Trim().ToLowerInvariant(), "").Trim().TrimEnd('.');
        if (DateOnly.TryParseExact(value, IsoFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var exact))
        {
            return exact;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var stamp) && value.Length >= 8 && char.IsDigit(value[0]))
        {
            return DateOnly.FromDateTime(stamp.Date);
        }

        switch (value)
        {
            case "today" or "tonight" or "end of day" or "eod":
                return today;
            case "tomorrow":
                return today.AddDays(1);
            case "next week":
                return NextMonday(today);
            case "end of week" or "end of the week" or "eow":
                return Weekday(today, DayOfWeek.Friday);
        }

        var relative = InDays().Match(value);
        if (relative.Success)
        {
            var count = int.Parse(relative.Groups["count"].Value, CultureInfo.InvariantCulture);
            return relative.Groups["unit"].Value.StartsWith("week", StringComparison.Ordinal) ? today.AddDays(count * 7) : today.AddDays(count);
        }

        var next = value.StartsWith("next ", StringComparison.Ordinal);
        var name = next ? value[5..].Trim() : value;
        if (Enum.TryParse<DayOfWeek>(name, ignoreCase: true, out var day) && !int.TryParse(name, out _))
        {
            return next ? Weekday(NextMonday(today), day) : Weekday(today, day);
        }

        return null;
    }

    public static TimeOnly? ResolveTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var value = text.Trim().ToLowerInvariant().Replace(".", "", StringComparison.Ordinal);
        string[] formats = ["H:mm", "HH:mm", "h:mm tt", "h:mmtt", "h tt", "htt", "%H"];
        return DateTime.TryParseExact(value.ToUpperInvariant(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? TimeOnly.FromDateTime(parsed)
            : null;
    }

    public static string Format(DateOnly date) => date.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);

    private static DateOnly Weekday(DateOnly from, DayOfWeek day) => from.AddDays(((int)day - (int)from.DayOfWeek + 7) % 7);

    private static DateOnly NextMonday(DateOnly today) => today.AddDays(7 - (((int)today.DayOfWeek + 6) % 7));

    [GeneratedRegex(@"^(by|on|due|before|until)\s+")]
    private static partial Regex LeadingWords();

    [GeneratedRegex(@"^in\s+(?<count>\d{1,3})\s+(?<unit>days?|weeks?)$")]
    private static partial Regex InDays();
}

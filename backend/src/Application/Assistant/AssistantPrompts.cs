using System.Globalization;
using System.Text.Json;

namespace Worknexa.Application.Assistant;

/// <summary>Prompt text lives in Assistant/Prompts as embedded files, apart from tool and service code.</summary>
public interface IAssistantPrompts
{
    string System(ToolContext context);

    string ToolDescription(string toolName);
}

public sealed class EmbeddedAssistantPrompts : IAssistantPrompts
{
    private static readonly Lazy<string> SystemTemplate = new(() => Load("assistant/system.md"));

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Descriptions = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(Load("assistant/tools.json")) ?? []);

    public string System(ToolContext context)
    {
        var offset = context.Offset;
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return SystemTemplate.Value
            .Replace("{{today}}", context.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{weekday}}", context.Today.DayOfWeek.ToString(), StringComparison.Ordinal)
            .Replace("{{utcOffset}}", $"{sign}{offset.Duration():hh\\:mm}", StringComparison.Ordinal);
    }

    public string ToolDescription(string toolName)
    {
        return Descriptions.Value.TryGetValue(toolName, out var description)
            ? description
            : throw new InvalidOperationException($"No prompt description exists for assistant tool '{toolName}'.");
    }

    private static string Load(string name)
    {
        using var stream = typeof(EmbeddedAssistantPrompts).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Assistant prompt '{name}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Results;

namespace Worknexa.Application.Assistant;

public enum ToolEffect
{
    Read,
    Write
}

public sealed record ToolContext(Guid UserId, IReadOnlyList<string> Permissions, DateTimeOffset Now, int OffsetMinutes)
{
    public TimeSpan Offset => TimeSpan.FromMinutes(OffsetMinutes);

    public DateOnly Today => DateOnly.FromDateTime(Now.ToOffset(Offset).DateTime);

    public bool Can(string permission) => Permissions.Contains(permission);

    public DateTimeOffset At(DateOnly date, TimeOnly time) => new DateTimeOffset(date.ToDateTime(time), Offset).ToUniversalTime();
}

public sealed record IntentField(string Label, string Value);

/// <summary>What an action tool would do, with every name already resolved. Nothing has run yet.</summary>
public sealed record ActionDraft(string Summary, IReadOnlyList<IntentField> Fields, JsonElement Payload, IReadOnlyList<string> Questions);

public sealed record ActionOutcome(string Message, string? Route);

public interface IAssistantTool
{
    string Name { get; }

    ToolEffect Effect { get; }

    /// <summary>Every key is required, matching the permission on the equivalent API endpoint.</summary>
    IReadOnlyList<string> Permissions { get; }

    JsonObject Parameters { get; }
}

public interface IReadTool : IAssistantTool
{
    Task<object> RunAsync(ToolContext context, JsonElement arguments, CancellationToken cancellationToken);
}

/// <summary>A consequential tool. It plans first; it only executes after the user confirms the plan.</summary>
public interface IActionTool : IAssistantTool
{
    Task<ActionDraft> PlanAsync(ToolContext context, JsonElement arguments, CancellationToken cancellationToken);

    Task<ActionOutcome> ExecuteAsync(ToolContext context, JsonElement payload, CancellationToken cancellationToken);
}

public abstract class ReadTool<TArgs> : IReadTool
{
    public abstract string Name { get; }

    public ToolEffect Effect => ToolEffect.Read;

    public abstract IReadOnlyList<string> Permissions { get; }

    public abstract JsonObject Parameters { get; }

    public Task<object> RunAsync(ToolContext context, JsonElement arguments, CancellationToken cancellationToken)
    {
        return RunAsync(context, ToolJson.Read<TArgs>(arguments), cancellationToken);
    }

    protected abstract Task<object> RunAsync(ToolContext context, TArgs args, CancellationToken cancellationToken);
}

public sealed record Planned<TPayload>(string Summary, IReadOnlyList<IntentField> Fields, TPayload? Payload, IReadOnlyList<string> Questions)
{
    public static Planned<TPayload> Ask(IReadOnlyList<string> questions) => new("", [], default, questions);
}

public abstract class ActionTool<TArgs, TPayload> : IActionTool
{
    public abstract string Name { get; }

    public ToolEffect Effect => ToolEffect.Write;

    public abstract IReadOnlyList<string> Permissions { get; }

    public abstract JsonObject Parameters { get; }

    public async Task<ActionDraft> PlanAsync(ToolContext context, JsonElement arguments, CancellationToken cancellationToken)
    {
        var planned = await PlanAsync(context, ToolJson.Read<TArgs>(arguments), cancellationToken);
        var payload = planned.Questions.Count == 0 && planned.Payload is not null ? ToolJson.Write(planned.Payload) : default;
        return new ActionDraft(planned.Summary, planned.Fields, payload, planned.Questions);
    }

    public Task<ActionOutcome> ExecuteAsync(ToolContext context, JsonElement payload, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, ToolJson.Read<TPayload>(payload), cancellationToken);
    }

    protected abstract Task<Planned<TPayload>> PlanAsync(ToolContext context, TArgs args, CancellationToken cancellationToken);

    protected abstract Task<ActionOutcome> ExecuteAsync(ToolContext context, TPayload payload, CancellationToken cancellationToken);
}

public static class ToolJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public static JsonElement Parse(string? json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }

    public static T Read<T>(JsonElement element)
    {
        try
        {
            var source = element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? Parse("{}") : element;
            return source.Deserialize<T>(Options) ?? throw Invalid();
        }
        catch (JsonException)
        {
            throw Invalid();
        }
    }

    public static JsonElement Write<T>(T value) => JsonSerializer.SerializeToElement(value, Options);

    public static string Text(object value) => JsonSerializer.Serialize(value, Options);

    private static ValidationException Invalid() => new([new ValidationFailure("arguments", "The tool arguments were not valid.")]);
}

public static class ToolSchema
{
    public static JsonObject Object(IReadOnlyList<string> required, params (string Name, JsonObject Schema)[] properties)
    {
        var props = new JsonObject();
        foreach (var (name, schema) in properties)
        {
            props[name] = schema;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = props,
            ["required"] = new JsonArray(required.Select(name => (JsonNode)JsonValue.Create(name)!).ToArray())
        };
    }

    public static JsonObject Text(string description) => new() { ["type"] = "string", ["description"] = description };

    public static JsonObject Flag(string description) => new() { ["type"] = "boolean", ["description"] = description };

    public static JsonObject Choice(string description, params string[] values) => new()
    {
        ["type"] = "string",
        ["description"] = description,
        ["enum"] = new JsonArray(values.Select(value => (JsonNode)JsonValue.Create(value)!).ToArray())
    };
}

public sealed class AssistantToolRegistry(IEnumerable<IAssistantTool> tools)
{
    private readonly IReadOnlyList<IAssistantTool> _tools = tools.OrderBy(tool => tool.Name, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<IAssistantTool> Available(IReadOnlyCollection<string> permissions)
    {
        return _tools.Where(tool => tool.Permissions.All(permissions.Contains)).ToArray();
    }

    public IAssistantTool? Find(string name)
    {
        return _tools.FirstOrDefault(tool => tool.Name == name);
    }
}

using System.Text.Json.Nodes;

namespace Worknexa.Application.Assistant;

public enum ModelRole
{
    System,
    User,
    Assistant,
    Tool
}

public sealed record ModelToolCall(string Id, string Name, string Arguments);

public sealed record ModelMessage(ModelRole Role, string? Content, IReadOnlyList<ModelToolCall>? ToolCalls = null, string? ToolCallId = null);

public sealed record ModelToolDefinition(string Name, string Description, JsonObject Parameters);

public sealed record ModelRequest(IReadOnlyList<ModelMessage> Messages, IReadOnlyList<ModelToolDefinition> Tools);

public sealed record ModelResponse(string? Content, IReadOnlyList<ModelToolCall> ToolCalls);

/// <summary>
/// A language model behind a provider-neutral contract. The assistant only ever sees this interface,
/// so OpenAI can be swapped for another provider by registering a different implementation.
/// </summary>
public interface IAssistantModel
{
    string Provider { get; }

    bool IsConfigured { get; }

    Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken);
}

public sealed class AssistantUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class AssistantSettings
{
    public const string SectionName = "Assistant";

    public int MaxToolRounds { get; set; } = 4;

    public int ProposalMinutes { get; set; } = 10;

    public int MaxHistoryMessages { get; set; } = 20;
}

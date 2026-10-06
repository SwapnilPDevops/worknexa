using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worknexa.Application.Assistant;

namespace Worknexa.Infrastructure.Assistant;

public sealed class OpenAiSettings
{
    public const string SectionName = "Assistant:OpenAI";

    /// <summary>Server-side only. Supply it through the Assistant__OpenAI__ApiKey environment variable or user secrets.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public int TimeoutSeconds { get; set; } = 60;

    public double Temperature { get; set; } = 0.2;
}

/// <summary>Adapter for the OpenAI Chat Completions API. Any compatible endpoint works by changing BaseUrl.</summary>
public sealed class OpenAiAssistantModel(HttpClient http, IOptions<OpenAiSettings> options, ILogger<OpenAiAssistantModel> logger) : IAssistantModel
{
    private readonly OpenAiSettings _settings = options.Value;

    public string Provider => "OpenAI";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new AssistantUnavailableException("The assistant is not configured yet.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(Body(request).ToJsonString(), Encoding.UTF8, "application/json")
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The assistant model could not be reached");
            throw new AssistantUnavailableException("The assistant service could not be reached. Try again shortly.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("The assistant model returned {StatusCode}", (int)response.StatusCode);
                throw new AssistantUnavailableException("The assistant service returned an error. Try again shortly.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return Parse(document.RootElement);
        }
    }

    private JsonObject Body(ModelRequest request)
    {
        var body = new JsonObject
        {
            ["model"] = _settings.Model,
            ["temperature"] = _settings.Temperature,
            ["messages"] = new JsonArray(request.Messages.Select(Message).ToArray<JsonNode?>())
        };

        if (request.Tools.Count > 0)
        {
            body["tools"] = new JsonArray(request.Tools.Select(tool => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = tool.Parameters.DeepClone()
                }
            }).ToArray<JsonNode?>());
        }

        return body;
    }

    private static JsonObject Message(ModelMessage message)
    {
        var node = new JsonObject
        {
            ["role"] = message.Role switch
            {
                ModelRole.System => "system",
                ModelRole.Assistant => "assistant",
                ModelRole.Tool => "tool",
                _ => "user"
            },
            ["content"] = message.Content
        };

        if (message.ToolCallId is not null)
        {
            node["tool_call_id"] = message.ToolCallId;
        }

        if (message.ToolCalls is { Count: > 0 } calls)
        {
            node["tool_calls"] = new JsonArray(calls.Select(call => (JsonNode)new JsonObject
            {
                ["id"] = call.Id,
                ["type"] = "function",
                ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.Arguments }
            }).ToArray<JsonNode?>());
        }

        return node;
    }

    private static ModelResponse Parse(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new AssistantUnavailableException("The assistant service returned an empty answer.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.TryGetProperty("content", out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null;
        var calls = new List<ModelToolCall>();
        if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in toolCalls.EnumerateArray())
            {
                var function = call.GetProperty("function");
                calls.Add(new ModelToolCall(
                    call.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
                    function.GetProperty("name").GetString() ?? "",
                    function.TryGetProperty("arguments", out var arguments) ? arguments.GetString() ?? "{}" : "{}"));
            }
        }

        return new ModelResponse(content, calls);
    }
}

/// <summary>Used when no provider is configured. The assistant reports itself as unavailable.</summary>
public sealed class DisabledAssistantModel : IAssistantModel
{
    public string Provider => "None";

    public bool IsConfigured => false;

    public Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        throw new AssistantUnavailableException("The assistant is not configured yet.");
    }
}

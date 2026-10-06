using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worknexa.Application.Common;

namespace Worknexa.Application.Assistant;

public sealed record AssistantChatMessage(string Role, string Content);

public sealed record AssistantChatRequest(IReadOnlyList<AssistantChatMessage> Messages, int OffsetMinutes = 0);

/// <summary>The structured intent of a consequential action, waiting for the person to confirm it.</summary>
public sealed record AssistantProposal(Guid Id, string Intent, string Summary, IReadOnlyList<IntentField> Fields, DateTimeOffset ExpiresAt);

public sealed record AssistantReply(string Message, IReadOnlyList<string> ToolsUsed, AssistantProposal? Proposal);

public sealed record AssistantActionResult(string Intent, string Message, string? Route);

public sealed record AssistantToolInfo(string Name, string Description, bool RequiresConfirmation);

public sealed record AssistantStatus(bool Enabled, string Provider, IReadOnlyList<AssistantToolInfo> Tools);

public sealed record PendingAction(Guid Id, Guid UserId, string Tool, JsonElement Payload, int OffsetMinutes, DateTimeOffset ExpiresAt);

public interface IAssistantActionStore
{
    void Save(PendingAction action);

    /// <summary>Removes and returns the action, so a proposal can run at most once.</summary>
    PendingAction? Take(Guid userId, Guid actionId, DateTimeOffset now);

    bool Remove(Guid userId, Guid actionId);
}

public interface IAssistantService
{
    AssistantStatus Status(IReadOnlyList<string> permissions);

    Task<AssistantReply> ChatAsync(Guid userId, IReadOnlyList<string> permissions, AssistantChatRequest request, DateTimeOffset now, CancellationToken cancellationToken);

    Task<AssistantActionResult> ConfirmAsync(Guid userId, IReadOnlyList<string> permissions, Guid actionId, DateTimeOffset now, CancellationToken cancellationToken);

    void Cancel(Guid userId, Guid actionId);
}

public sealed class AssistantService(
    IAssistantModel model,
    AssistantToolRegistry registry,
    IAssistantPrompts prompts,
    IAssistantActionStore actions,
    IOptions<AssistantSettings> options,
    ILogger<AssistantService> logger) : IAssistantService
{
    private const int MaxMessageLength = 4000;
    private const int MaxToolResultLength = 12000;

    private readonly AssistantSettings _settings = options.Value;

    public AssistantStatus Status(IReadOnlyList<string> permissions)
    {
        var tools = registry.Available(permissions)
            .Select(tool => new AssistantToolInfo(tool.Name, prompts.ToolDescription(tool.Name), tool.Effect == ToolEffect.Write))
            .ToArray();
        return new AssistantStatus(model.IsConfigured, model.Provider, tools);
    }

    public async Task<AssistantReply> ChatAsync(Guid userId, IReadOnlyList<string> permissions, AssistantChatRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Validate(request);
        if (!model.IsConfigured)
        {
            throw new AssistantUnavailableException("The assistant is not configured yet. An administrator needs to add a model provider key on the server.");
        }

        var context = new ToolContext(userId, permissions, now, request.OffsetMinutes);
        var tools = registry.Available(permissions);
        var definitions = tools.Select(tool => new ModelToolDefinition(tool.Name, prompts.ToolDescription(tool.Name), tool.Parameters)).ToArray();
        var messages = new List<ModelMessage> { new(ModelRole.System, prompts.System(context)) };
        messages.AddRange(request.Messages
            .TakeLast(Math.Max(1, _settings.MaxHistoryMessages))
            .Select(message => new ModelMessage(message.Role == "assistant" ? ModelRole.Assistant : ModelRole.User, message.Content.Trim())));
        var used = new List<string>();

        for (var round = 0; round < Math.Max(1, _settings.MaxToolRounds); round++)
        {
            var response = await model.CompleteAsync(new ModelRequest(messages, definitions), cancellationToken);
            if (response.ToolCalls.Count == 0)
            {
                return new AssistantReply(string.IsNullOrWhiteSpace(response.Content) ? "I could not find an answer to that." : response.Content.Trim(), used, null);
            }

            messages.Add(new ModelMessage(ModelRole.Assistant, response.Content, response.ToolCalls));
            foreach (var call in response.ToolCalls)
            {
                used.Add(call.Name);
                var tool = tools.FirstOrDefault(item => item.Name == call.Name);
                object result;
                try
                {
                    var arguments = ToolJson.Parse(call.Arguments);
                    switch (tool)
                    {
                        case IReadTool read:
                            result = await read.RunAsync(context, arguments, cancellationToken);
                            break;
                        case IActionTool action:
                            var draft = await action.PlanAsync(context, arguments, cancellationToken);
                            if (draft.Questions.Count == 0)
                            {
                                return new AssistantReply($"{draft.Summary} Confirm to go ahead.", used, Propose(userId, action, draft, request.OffsetMinutes, now));
                            }

                            result = new { status = "needs_input", questions = draft.Questions };
                            break;
                        default:
                            result = new { error = "That tool is not available to you." };
                            break;
                    }
                }
                catch (Exception exception) when (exception is ValidationException or NotFoundException or ConflictException or ForbiddenException or JsonException)
                {
                    result = new { error = Describe(exception) };
                }

                messages.Add(new ModelMessage(ModelRole.Tool, Truncate(ToolJson.Text(result)), ToolCallId: call.Id));
            }
        }

        return new AssistantReply("I could not finish that in a few steps. Try a more specific request.", used, null);
    }

    public async Task<AssistantActionResult> ConfirmAsync(Guid userId, IReadOnlyList<string> permissions, Guid actionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var pending = actions.Take(userId, actionId, now)
            ?? throw new NotFoundException("This action has expired or was already handled. Ask the assistant again.");
        if (registry.Find(pending.Tool) is not IActionTool tool)
        {
            throw new NotFoundException("This action is no longer available.");
        }

        if (!tool.Permissions.All(permissions.Contains))
        {
            throw new ForbiddenException("You do not have permission to do that.");
        }

        var outcome = await tool.ExecuteAsync(new ToolContext(userId, permissions, now, pending.OffsetMinutes), pending.Payload, cancellationToken);
        logger.LogInformation("Assistant action {Tool} confirmed by {UserId}", tool.Name, userId);
        return new AssistantActionResult(tool.Name, outcome.Message, outcome.Route);
    }

    public void Cancel(Guid userId, Guid actionId)
    {
        actions.Remove(userId, actionId);
    }

    private AssistantProposal Propose(Guid userId, IActionTool tool, ActionDraft draft, int offsetMinutes, DateTimeOffset now)
    {
        var pending = new PendingAction(Guid.NewGuid(), userId, tool.Name, draft.Payload, offsetMinutes, now.AddMinutes(Math.Max(1, _settings.ProposalMinutes)));
        actions.Save(pending);
        return new AssistantProposal(pending.Id, tool.Name, draft.Summary, draft.Fields, pending.ExpiresAt);
    }

    private static void Validate(AssistantChatRequest request)
    {
        var failures = new List<ValidationFailure>();
        if (request.Messages is not { Count: > 0 } || request.Messages[^1].Role != "user")
        {
            failures.Add(new ValidationFailure("messages", "End the conversation with a message from the user."));
        }
        else if (request.Messages.Any(message => message.Role is not ("user" or "assistant") || string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > MaxMessageLength))
        {
            failures.Add(new ValidationFailure("messages", $"Messages need a user or assistant role and 1 to {MaxMessageLength} characters."));
        }

        if (request.OffsetMinutes is < -840 or > 840)
        {
            failures.Add(new ValidationFailure("offsetMinutes", "Offset is out of range."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }

    private static string Describe(Exception exception)
    {
        return exception is ValidationException validation
            ? string.Join(" ", validation.Errors.Select(error => error.ErrorMessage).Distinct())
            : exception is JsonException ? "The tool arguments were not valid JSON." : exception.Message;
    }

    private static string Truncate(string text) => text.Length <= MaxToolResultLength ? text : text[..MaxToolResultLength];
}

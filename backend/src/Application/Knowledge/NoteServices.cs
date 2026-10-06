using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Work;

namespace Worknexa.Application.Knowledge;

public sealed record NoteItem(
    Guid Id,
    string Title,
    string Content,
    string? Category,
    bool IsFavorite,
    bool IsPinned,
    Guid? ProjectId,
    string? ProjectName,
    Guid? TaskId,
    string? TaskTitle,
    Guid? MeetingId,
    string? MeetingTitle,
    Guid? LearningTopicId,
    string? LearningTopicTitle,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NoteWrite(
    string Title,
    string Content,
    string? Category,
    bool IsFavorite,
    bool IsPinned,
    Guid? ProjectId,
    Guid? TaskId,
    Guid? MeetingId,
    Guid? LearningTopicId,
    IReadOnlyList<string>? Tags);

public sealed record NoteOptions(
    IReadOnlyList<NamedRef> Projects,
    IReadOnlyList<NamedRef> Tasks,
    IReadOnlyList<NamedRef> Meetings,
    IReadOnlyList<NamedRef> LearningTopics,
    IReadOnlyList<string> Tags);

public interface INoteStore
{
    Task<NoteOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoteItem>> ListAsync(Guid userId, string? query, Guid? learningTopicId, bool learningOnly, bool favoritesOnly, CancellationToken cancellationToken);

    Task<NoteItem?> FindAsync(Guid userId, Guid noteId, CancellationToken cancellationToken);

    Task<NoteItem> CreateAsync(Guid userId, NoteWrite request, IReadOnlyList<string> tags, CancellationToken cancellationToken);

    Task<NoteItem?> UpdateAsync(Guid userId, Guid noteId, NoteWrite request, IReadOnlyList<string> tags, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken);
}

public interface INoteService
{
    Task<NoteOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoteItem>> ListAsync(Guid userId, string? query, Guid? learningTopicId, bool learningOnly, bool favoritesOnly, CancellationToken cancellationToken);

    Task<NoteItem> GetAsync(Guid userId, Guid noteId, CancellationToken cancellationToken);

    Task<NoteItem> CreateAsync(Guid userId, NoteWrite request, CancellationToken cancellationToken);

    Task<NoteItem> UpdateAsync(Guid userId, Guid noteId, NoteWrite request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken);
}

public sealed class NoteService(INoteStore store) : INoteService
{
    public Task<NoteOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.OptionsAsync(userId, cancellationToken);
    }

    public Task<IReadOnlyList<NoteItem>> ListAsync(Guid userId, string? query, Guid? learningTopicId, bool learningOnly, bool favoritesOnly, CancellationToken cancellationToken)
    {
        var term = query?.Trim();
        return store.ListAsync(userId, string.IsNullOrEmpty(term) ? null : term, learningTopicId, learningOnly, favoritesOnly, cancellationToken);
    }

    public async Task<NoteItem> GetAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, noteId, cancellationToken) ?? throw new NotFoundException("Note was not found.");
    }

    public Task<NoteItem> CreateAsync(Guid userId, NoteWrite request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, Normalize(request), TagNames.Normalize(request.Tags), cancellationToken);
    }

    public async Task<NoteItem> UpdateAsync(Guid userId, Guid noteId, NoteWrite request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, noteId, Normalize(request), TagNames.Normalize(request.Tags), cancellationToken)
            ?? throw new NotFoundException("Note was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, noteId, cancellationToken))
        {
            throw new NotFoundException("Note was not found.");
        }
    }

    private static NoteWrite Normalize(NoteWrite request)
    {
        var category = request.Category?.Trim();
        return request with
        {
            Title = request.Title.Trim(),
            Content = request.Content ?? string.Empty,
            Category = string.IsNullOrEmpty(category) ? null : category
        };
    }
}

public sealed class NoteWriteValidator : AbstractValidator<NoteWrite>
{
    public NoteWriteValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Content).MaximumLength(100_000);
        RuleFor(request => request.Category).MaximumLength(80);
    }
}

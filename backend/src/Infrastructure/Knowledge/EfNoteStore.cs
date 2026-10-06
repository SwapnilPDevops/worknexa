using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Knowledge;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Knowledge;
using Worknexa.Domain.Productivity;
using Worknexa.Infrastructure.Persistence;
using Worknexa.Infrastructure.Work;

namespace Worknexa.Infrastructure.Knowledge;

public sealed class EfNoteStore(WorknexaDbContext context) : INoteStore
{
    public async Task<NoteOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var projects = await context.Projects.AsNoTracking()
            .Where(project => project.Status == ProjectStatus.Planning || project.Status == ProjectStatus.Active || project.Status == ProjectStatus.OnHold)
            .OrderBy(project => project.Name)
            .Select(project => new NamedRef(project.Id, project.Name))
            .ToListAsync(cancellationToken);
        var tasks = await VisibleTasks(userId).AsNoTracking()
            .OrderBy(task => task.Title)
            .Select(task => new NamedRef(task.Id, task.Title))
            .Take(200)
            .ToListAsync(cancellationToken);
        var meetings = await VisibleMeetings(userId).AsNoTracking()
            .OrderByDescending(meeting => meeting.StartAt)
            .Select(meeting => new NamedRef(meeting.Id, meeting.Title))
            .Take(200)
            .ToListAsync(cancellationToken);
        var topics = await context.LearningTopics.AsNoTracking()
            .Where(topic => topic.UserId == userId)
            .OrderBy(topic => topic.Title)
            .Select(topic => new NamedRef(topic.Id, topic.Title))
            .Take(200)
            .ToListAsync(cancellationToken);
        var tags = await context.Tags.AsNoTracking()
            .OrderBy(tag => tag.Name)
            .Select(tag => tag.Name)
            .Take(200)
            .ToListAsync(cancellationToken);
        return new NoteOptions(projects, tasks, meetings, topics, tags);
    }

    public async Task<IReadOnlyList<NoteItem>> ListAsync(Guid userId, string? query, Guid? learningTopicId, bool learningOnly, bool favoritesOnly, CancellationToken cancellationToken)
    {
        var notes = Owned(userId);
        if (learningTopicId is Guid topicId)
        {
            notes = notes.Where(note => note.LearningTopicId == topicId);
        }

        if (learningOnly)
        {
            notes = notes.Where(note => note.LearningTopicId != null);
        }

        if (favoritesOnly)
        {
            notes = notes.Where(note => note.IsFavorite);
        }

        if (!string.IsNullOrEmpty(query))
        {
            notes = notes.Where(note =>
                note.Title.Contains(query)
                || note.Body.Contains(query)
                || (note.Category != null && note.Category.Contains(query))
                || note.NoteTags.Any(noteTag => noteTag.Tag.Name.Contains(query)));
        }

        var rows = await notes
            .OrderByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.IsFavorite)
            .ThenByDescending(note => note.UpdatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return await MapManyAsync(rows, cancellationToken);
    }

    public async Task<NoteItem?> FindAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        var note = await Owned(userId).FirstOrDefaultAsync(item => item.Id == noteId, cancellationToken);
        return note is null ? null : await MapAsync(note, cancellationToken);
    }

    public async Task<NoteItem> CreateAsync(Guid userId, NoteWrite request, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        await EnsureAsync(userId, request, cancellationToken);
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        Apply(note, request);
        context.Notes.Add(note);
        await ReplaceTagsAsync(note, tags, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, note.Id, cancellationToken))!;
    }

    public async Task<NoteItem?> UpdateAsync(Guid userId, Guid noteId, NoteWrite request, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var note = await Owned(userId).FirstOrDefaultAsync(item => item.Id == noteId, cancellationToken);
        if (note is null)
        {
            return null;
        }

        await EnsureAsync(userId, request, cancellationToken);
        Apply(note, request);
        await ReplaceTagsAsync(note, tags, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, note.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        var note = await Owned(userId).FirstOrDefaultAsync(item => item.Id == noteId, cancellationToken);
        if (note is null)
        {
            return false;
        }

        note.DeletedAt = DateTimeOffset.UtcNow;
        note.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Note> Owned(Guid userId)
    {
        return context.Notes.Where(note => note.UserId == userId);
    }

    private IQueryable<WorkTask> VisibleTasks(Guid userId)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var projectIds = context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
        return context.Tasks.Where(task =>
            task.AssigneeId == userId
            || task.CreatedBy == userId
            || (task.TeamId != null && teamIds.Contains(task.TeamId.Value))
            || (task.ProjectId != null && projectIds.Contains(task.ProjectId.Value)));
    }

    private IQueryable<Domain.Meetings.Meeting> VisibleMeetings(Guid userId)
    {
        return context.Meetings.Where(meeting =>
            meeting.OrganizerId == userId
            || meeting.Participants.Any(participant => participant.UserId == userId)
            || (meeting.TeamId != null && context.TeamMembers.Any(member => member.UserId == userId && member.TeamId == meeting.TeamId)));
    }

    private async Task EnsureAsync(Guid userId, NoteWrite request, CancellationToken cancellationToken)
    {
        if (request.ProjectId is Guid projectId && !await context.Projects.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw new ConflictException("Choose a project that exists.");
        }

        if (request.TaskId is Guid taskId && !await VisibleTasks(userId).AnyAsync(task => task.Id == taskId, cancellationToken))
        {
            throw new ConflictException("Choose a task you can see.");
        }

        if (request.MeetingId is Guid meetingId && !await VisibleMeetings(userId).AnyAsync(meeting => meeting.Id == meetingId, cancellationToken))
        {
            throw new ConflictException("Choose a meeting you can see.");
        }

        if (request.LearningTopicId is Guid topicId && !await context.LearningTopics.AnyAsync(topic => topic.Id == topicId && topic.UserId == userId, cancellationToken))
        {
            throw new ConflictException("Choose a learning topic you own.");
        }
    }

    private static void Apply(Note note, NoteWrite request)
    {
        note.Title = request.Title;
        note.Body = request.Content;
        note.Category = request.Category;
        note.IsFavorite = request.IsFavorite;
        note.IsPinned = request.IsPinned;
        note.ProjectId = request.ProjectId;
        note.TaskId = request.TaskId;
        note.MeetingId = request.MeetingId;
        note.LearningTopicId = request.LearningTopicId;
    }

    private async Task ReplaceTagsAsync(Note note, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        var tags = await TagDirectory.ResolveAsync(context, names, cancellationToken);
        var wanted = tags.Select(tag => tag.Id).ToHashSet();
        var existing = await context.NoteTags.Where(noteTag => noteTag.NoteId == note.Id).ToListAsync(cancellationToken);
        context.NoteTags.RemoveRange(existing.Where(noteTag => !wanted.Contains(noteTag.TagId)));
        foreach (var tag in tags.Where(tag => existing.All(noteTag => noteTag.TagId != tag.Id)))
        {
            var link = new NoteTag
            {
                Id = Guid.NewGuid(),
                NoteId = note.Id,
                TagId = tag.Id
            };
            context.NoteTags.Add(link);
            context.Entry(link).State = EntityState.Added;
        }
    }

    private async Task<IReadOnlyList<NoteItem>> MapManyAsync(IReadOnlyList<Note> notes, CancellationToken cancellationToken)
    {
        var items = new List<NoteItem>(notes.Count);
        foreach (var note in notes)
        {
            items.Add(await MapAsync(note, cancellationToken));
        }

        return items;
    }

    private async Task<NoteItem> MapAsync(Note note, CancellationToken cancellationToken)
    {
        var tags = await context.NoteTags.AsNoTracking()
            .Where(noteTag => noteTag.NoteId == note.Id)
            .Select(noteTag => noteTag.Tag.Name)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
        var projectName = note.ProjectId is Guid projectId
            ? await context.Projects.Where(project => project.Id == projectId).Select(project => project.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var taskTitle = note.TaskId is Guid taskId
            ? await context.Tasks.Where(task => task.Id == taskId).Select(task => task.Title).FirstOrDefaultAsync(cancellationToken)
            : null;
        var meetingTitle = note.MeetingId is Guid meetingId
            ? await context.Meetings.Where(meeting => meeting.Id == meetingId).Select(meeting => meeting.Title).FirstOrDefaultAsync(cancellationToken)
            : null;
        var topicTitle = note.LearningTopicId is Guid topicId
            ? await context.LearningTopics.Where(topic => topic.Id == topicId).Select(topic => topic.Title).FirstOrDefaultAsync(cancellationToken)
            : null;
        return new NoteItem(
            note.Id,
            note.Title,
            note.Body,
            note.Category,
            note.IsFavorite,
            note.IsPinned,
            note.ProjectId,
            projectName,
            note.TaskId,
            taskTitle,
            note.MeetingId,
            meetingTitle,
            note.LearningTopicId,
            topicTitle,
            tags,
            note.CreatedAt,
            note.UpdatedAt);
    }
}

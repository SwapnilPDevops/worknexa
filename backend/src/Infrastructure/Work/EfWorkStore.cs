using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Worknexa.Application.Common;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Platform;
using Worknexa.Domain.Productivity;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Work;

public sealed class EfTaskStore(WorknexaDbContext context, IConfiguration configuration) : ITaskStore
{
    public async Task<IReadOnlyList<TaskListItem>> ListAsync(Guid userId, TaskFilter filter, CancellationToken cancellationToken)
    {
        var query = Visible(userId);
        if (filter.Status is not null)
        {
            query = query.Where(task => task.Status == filter.Status);
        }

        if (filter.Priority is not null)
        {
            query = query.Where(task => task.Priority == filter.Priority);
        }

        if (filter.ProjectId is not null)
        {
            query = query.Where(task => task.ProjectId == filter.ProjectId);
        }

        if (filter.AssigneeId is not null || filter.Mine)
        {
            var assigneeId = filter.Mine ? userId : filter.AssigneeId;
            query = query.Where(task => task.AssigneeId == assigneeId);
        }

        if (filter.Team)
        {
            var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
            query = query.Where(task => task.TeamId != null && teamIds.Contains(task.TeamId.Value));
        }

        if (filter.TeamId is not null)
        {
            query = query.Where(task => task.TeamId == filter.TeamId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            query = query.Where(task => task.TaskTags.Any(taskTag => taskTag.Tag.Name == filter.Tag));
        }

        if (filter.DueFrom is not null)
        {
            query = query.Where(task => task.DueAt != null && task.DueAt >= filter.DueFrom);
        }

        if (filter.DueTo is not null)
        {
            query = query.Where(task => task.DueAt != null && task.DueAt < filter.DueTo);
        }

        if (filter.OverdueBefore is not null)
        {
            query = query.Where(task =>
                task.Status != WorkTaskStatus.Completed
                && task.Status != WorkTaskStatus.Cancelled
                && task.DueAt != null
                && task.DueAt < filter.OverdueBefore);
        }

        return await query
            .OrderBy(task => task.DueAt)
            .ThenBy(task => task.Title)
            .Take(300)
            .Select(task => new TaskListItem(
                task.Id,
                task.Title,
                task.Status.ToString(),
                task.Priority.ToString(),
                task.StartAt,
                task.DueAt,
                task.AssigneeId,
                task.Assignee != null ? task.Assignee.DisplayName : null,
                task.ProjectId,
                task.Project != null ? task.Project.Name : null,
                task.ParentTaskId,
                task.TeamId,
                task.EstimatedHours,
                task.ActualHours,
                task.TaskTags.Select(taskTag => taskTag.Tag.Name).ToList()))
            .ToListAsync(cancellationToken);
    }

    public Task<TaskDetail?> FindAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return ProjectDetail(Visible(userId).Where(task => task.Id == taskId), context).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var projects = await context.Projects.AsNoTracking()
            .Where(project => project.Status == ProjectStatus.Planning || project.Status == ProjectStatus.Active || project.Status == ProjectStatus.OnHold)
            .OrderBy(project => project.Name)
            .Select(project => new NamedRef(project.Id, project.Name))
            .ToListAsync(cancellationToken);
        var people = await context.Users.AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .Select(user => new NamedRef(user.Id, user.DisplayName))
            .ToListAsync(cancellationToken);
        var teams = await context.TeamMembers.AsNoTracking()
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.Team.Name)
            .Select(member => new NamedRef(member.TeamId, member.Team.Name))
            .ToListAsync(cancellationToken);
        var tags = await context.Tags.AsNoTracking()
            .OrderBy(tag => tag.Name)
            .Select(tag => tag.Name)
            .Take(200)
            .ToListAsync(cancellationToken);
        return new WorkOptions(projects, people, teams, tags);
    }

    public async Task<TaskDetail> CreateAsync(Guid userId, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken)
    {
        var task = new WorkTask { Id = Guid.NewGuid() };
        await ApplyAsync(userId, task, request, status, priority, cancellationToken);
        context.Tasks.Add(task);
        await ReplaceTagsAsync(task, request.Tags, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, task.Id, cancellationToken))!;
    }

    public async Task<TaskDetail?> UpdateAsync(Guid userId, Guid taskId, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken)
    {
        var task = await Visible(userId).FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        await ApplyAsync(userId, task, request, status, priority, cancellationToken);
        await ReplaceTagsAsync(task, request.Tags, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, task.Id, cancellationToken);
    }

    public async Task<TaskDetail?> SetStatusAsync(Guid userId, Guid taskId, WorkTaskStatus status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var task = await Visible(userId).FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        ApplyStatus(task, status, now);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, task.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await Visible(userId).FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        task.DeletedAt = now;
        task.DeletedBy = userId;
        var attachments = await context.Attachments
            .Where(attachment => attachment.EntityName == WorkConventions.TaskAttachment && attachment.EntityId == taskId)
            .ToListAsync(cancellationToken);
        foreach (var attachment in attachments)
        {
            attachment.DeletedAt = now;
            attachment.DeletedBy = userId;
            AttachmentFiles.Delete(configuration, attachment.StorageKey);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TaskDetail?> AssignAsync(Guid userId, Guid taskId, Guid? assigneeId, CancellationToken cancellationToken)
    {
        var task = await Visible(userId).FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        await EnsureAssigneeAsync(assigneeId, cancellationToken);
        task.AssigneeId = assigneeId;
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, task.Id, cancellationToken);
    }

    public async Task<TaskCommentItem?> CommentAsync(Guid userId, Guid taskId, string body, CancellationToken cancellationToken)
    {
        var task = await Visible(userId).FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            Body = body
        };
        context.TaskComments.Add(comment);
        await context.SaveChangesAsync(cancellationToken);
        var author = await context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
        return new TaskCommentItem(comment.Id, comment.Body, author ?? "Someone", comment.CreatedAt);
    }

    public async Task<TaskDetail?> AddDependencyAsync(Guid userId, Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken)
    {
        var task = await Visible(userId).FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (!await context.Tasks.AnyAsync(item => item.Id == dependsOnTaskId, cancellationToken))
        {
            throw new ConflictException("The dependency task was not found.");
        }

        var edges = await context.TaskDependencies.AsNoTracking()
            .Select(dependency => new { dependency.TaskId, dependency.DependsOnTaskId })
            .ToListAsync(cancellationToken);
        var links = edges
            .GroupBy(edge => edge.TaskId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)group.Select(edge => edge.DependsOnTaskId).ToArray());
        if (WorkGraphs.CreatesCycle(taskId, dependsOnTaskId, links))
        {
            throw new ConflictException("That dependency would create a cycle.");
        }

        if (edges.Any(edge => edge.TaskId == taskId && edge.DependsOnTaskId == dependsOnTaskId))
        {
            throw new ConflictException("That dependency already exists.");
        }

        context.TaskDependencies.Add(new TaskDependency
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            DependsOnTaskId = dependsOnTaskId
        });
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, taskId, cancellationToken);
    }

    public async Task<bool> RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependencyId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(task => task.Id == taskId, cancellationToken))
        {
            return false;
        }

        var dependency = await context.TaskDependencies.FirstOrDefaultAsync(
            item => item.Id == dependencyId && item.TaskId == taskId,
            cancellationToken);
        if (dependency is null)
        {
            return false;
        }

        context.TaskDependencies.Remove(dependency);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TaskFile?> AddAttachmentAsync(Guid userId, Guid taskId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(task => task.Id == taskId, cancellationToken))
        {
            return null;
        }

        var safeName = string.IsNullOrWhiteSpace(fileName) ? "attachment" : fileName.Trim();
        if (safeName.Length > 260)
        {
            safeName = safeName[^260..];
        }

        var storageKey = $"{taskId:N}/{Guid.NewGuid():N}";
        var size = await AttachmentFiles.SaveAsync(configuration, storageKey, content, cancellationToken);
        if (size > 10 * 1024 * 1024)
        {
            AttachmentFiles.Delete(configuration, storageKey);
            throw new FluentValidation.ValidationException([new FluentValidation.Results.ValidationFailure("file", "Attachments must be 10 MB or smaller.")]);
        }
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            EntityName = WorkConventions.TaskAttachment,
            EntityId = taskId,
            FileName = safeName,
            ContentType = contentType.Length <= 128 ? contentType : contentType[..128],
            SizeBytes = size,
            StorageKey = storageKey
        };
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(cancellationToken);
        return new TaskFile(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes);
    }

    public async Task<StoredAttachment?> OpenAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(task => task.Id == taskId, cancellationToken))
        {
            return null;
        }

        var attachment = await context.Attachments.AsNoTracking().FirstOrDefaultAsync(
            item => item.Id == attachmentId && item.EntityName == WorkConventions.TaskAttachment && item.EntityId == taskId,
            cancellationToken);
        if (attachment is null)
        {
            return null;
        }

        var stream = AttachmentFiles.Open(configuration, attachment.StorageKey);
        return stream is null
            ? null
            : new StoredAttachment(new TaskFile(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes), stream);
    }

    public async Task<bool> RemoveAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(task => task.Id == taskId, cancellationToken))
        {
            return false;
        }

        var attachment = await context.Attachments.FirstOrDefaultAsync(
            item => item.Id == attachmentId && item.EntityName == WorkConventions.TaskAttachment && item.EntityId == taskId,
            cancellationToken);
        if (attachment is null)
        {
            return false;
        }

        attachment.DeletedAt = DateTimeOffset.UtcNow;
        attachment.DeletedBy = userId;
        AttachmentFiles.Delete(configuration, attachment.StorageKey);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ApplyAsync(Guid userId, WorkTask task, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken)
    {
        await EnsureAssigneeAsync(request.AssigneeId, cancellationToken);
        if (request.ProjectId is Guid projectId
            && !await context.Projects.AnyAsync(project => project.Id == projectId && (project.Status == ProjectStatus.Planning || project.Status == ProjectStatus.Active || project.Status == ProjectStatus.OnHold), cancellationToken))
        {
            throw new ConflictException("Choose a project that is still open.");
        }

        if (request.TeamId is Guid teamId
            && !await context.TeamMembers.AnyAsync(member => member.TeamId == teamId && member.UserId == userId, cancellationToken))
        {
            throw new ConflictException("Join a team before assigning a team task.");
        }

        if (request.ParentTaskId is Guid parentId)
        {
            if (!await context.Tasks.AnyAsync(item => item.Id == parentId, cancellationToken))
            {
                throw new ConflictException("Parent task was not found.");
            }

            var parents = await context.Tasks.AsNoTracking()
                .Where(item => item.ParentTaskId != null)
                .Select(item => new { item.Id, ParentId = item.ParentTaskId!.Value })
                .ToListAsync(cancellationToken);
            var links = parents.ToDictionary(item => item.Id, item => (IReadOnlyList<Guid>)[item.ParentId]);
            if (WorkGraphs.CreatesCycle(task.Id, parentId, links))
            {
                throw new ConflictException("A task cannot contain itself.");
            }
        }

        task.Title = request.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        task.Priority = priority;
        task.StartAt = request.StartAt;
        task.DueAt = request.DueAt;
        task.AssigneeId = request.AssigneeId;
        task.ProjectId = request.ProjectId;
        task.ParentTaskId = request.ParentTaskId;
        task.TeamId = request.TeamId;
        task.EstimatedHours = request.EstimatedHours;
        task.ActualHours = request.ActualHours;
        ApplyStatus(task, status, DateTimeOffset.UtcNow);
    }

    private async Task EnsureAssigneeAsync(Guid? assigneeId, CancellationToken cancellationToken)
    {
        if (assigneeId is Guid id && !await context.Users.AnyAsync(user => user.Id == id && user.IsActive, cancellationToken))
        {
            throw new ConflictException("Choose an active person.");
        }
    }

    private async Task ReplaceTagsAsync(WorkTask task, IReadOnlyList<string>? names, CancellationToken cancellationToken)
    {
        var normalized = TagNames.Normalize(names);
        var tags = await TagDirectory.ResolveAsync(context, normalized, cancellationToken);
        var wanted = tags.Select(tag => tag.Id).ToHashSet();
        var existing = await context.TaskTags.Where(taskTag => taskTag.TaskId == task.Id).ToListAsync(cancellationToken);
        context.TaskTags.RemoveRange(existing.Where(taskTag => !wanted.Contains(taskTag.TagId)));
        foreach (var tag in tags.Where(tag => existing.All(taskTag => taskTag.TagId != tag.Id)))
        {
            context.TaskTags.Add(new TaskTag
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                TagId = tag.Id
            });
        }
    }

    private IQueryable<WorkTask> Visible(Guid userId)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var projectIds = context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
        return context.Tasks.Where(task =>
            task.AssigneeId == userId
            || task.CreatedBy == userId
            || (task.TeamId != null && teamIds.Contains(task.TeamId.Value))
            || (task.ProjectId != null && projectIds.Contains(task.ProjectId.Value)));
    }

    private static IQueryable<TaskDetail> ProjectDetail(IQueryable<WorkTask> tasks, WorknexaDbContext context)
    {
        return tasks.Select(task => new TaskDetail(
            task.Id,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.Priority.ToString(),
            task.StartAt,
            task.DueAt,
            task.AssigneeId,
            task.Assignee != null ? task.Assignee.DisplayName : null,
            task.ProjectId,
            task.Project != null ? task.Project.Name : null,
            task.ParentTaskId,
            task.ParentTask != null ? task.ParentTask.Title : null,
            task.TeamId,
            task.EstimatedHours,
            task.ActualHours,
            task.TaskTags.Select(taskTag => taskTag.Tag.Name).ToList(),
            task.Subtasks.OrderBy(child => child.SortOrder).ThenBy(child => child.Title).Select(child => new TaskChild(child.Id, child.Title, child.Status.ToString())).ToList(),
            task.Dependencies.Select(dependency => new TaskLink(
                dependency.Id,
                dependency.DependsOnTaskId,
                dependency.DependsOnTask.Title,
                dependency.DependsOnTask.Status.ToString())).ToList(),
            task.Comments.OrderBy(comment => comment.CreatedAt).Select(comment => new TaskCommentItem(
                comment.Id,
                comment.Body,
                context.Users.Where(user => user.Id == comment.CreatedBy).Select(user => user.DisplayName).FirstOrDefault() ?? "Someone",
                comment.CreatedAt)).ToList(),
            context.Attachments
                .Where(attachment => attachment.EntityName == WorkConventions.TaskAttachment && attachment.EntityId == task.Id)
                .Select(attachment => new TaskFile(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes))
                .ToList(),
            task.MeetingId,
            task.Meeting != null ? task.Meeting.Title : null));
    }

    private static void ApplyStatus(WorkTask task, WorkTaskStatus status, DateTimeOffset now)
    {
        task.Status = status;
        task.CompletedAt = status == WorkTaskStatus.Completed ? task.CompletedAt ?? now : null;
    }
}

public sealed class EfTodoStore(WorknexaDbContext context) : ITodoStore
{
    public async Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.TodoItems.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.DueAt)
            .ThenBy(item => item.Title)
            .Select(item => new TodoItemDto(
                item.Id,
                item.Title,
                item.IsCompleted,
                item.Priority.ToString(),
                item.Recurrence.ToString(),
                item.DueAt,
                context.Reminders
                    .Where(reminder => reminder.TodoItemId == item.Id)
                    .OrderBy(reminder => reminder.RemindAt)
                    .Select(reminder => (DateTimeOffset?)reminder.RemindAt)
                    .FirstOrDefault(),
                "",
                item.TodoTags.Select(todoTag => todoTag.Tag.Name).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<TodoItemDto> CreateAsync(Guid userId, TodoDraft draft, CancellationToken cancellationToken)
    {
        var item = new TodoItem { Id = Guid.NewGuid(), UserId = userId };
        Apply(item, draft);
        context.TodoItems.Add(item);
        await ReplaceTagsAsync(item, draft.Tags, cancellationToken);
        await ReplaceReminderAsync(userId, item, draft.RemindAt, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, item.Id, cancellationToken))!;
    }

    public async Task<TodoItemDto?> UpdateAsync(Guid userId, Guid todoId, TodoDraft draft, CancellationToken cancellationToken)
    {
        var item = await context.TodoItems.FirstOrDefaultAsync(todo => todo.Id == todoId && todo.UserId == userId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        Apply(item, draft);
        await ReplaceTagsAsync(item, draft.Tags, cancellationToken);
        await ReplaceReminderAsync(userId, item, draft.RemindAt, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, item.Id, cancellationToken);
    }

    public async Task<TodoItemDto?> CompleteAsync(Guid userId, Guid todoId, TodoDraft? next, CancellationToken cancellationToken)
    {
        var item = await context.TodoItems.FirstOrDefaultAsync(todo => todo.Id == todoId && todo.UserId == userId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.IsCompleted = true;
        if (next is not null)
        {
            var followUp = new TodoItem { Id = Guid.NewGuid(), UserId = userId };
            Apply(followUp, next);
            context.TodoItems.Add(followUp);
            await ReplaceTagsAsync(followUp, next.Tags, cancellationToken);
            await ReplaceReminderAsync(userId, followUp, next.RemindAt, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, item.Id, cancellationToken);
    }

    public async Task<TodoItemDto?> ReopenAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        var item = await context.TodoItems.FirstOrDefaultAsync(todo => todo.Id == todoId && todo.UserId == userId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.IsCompleted = false;
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, item.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        var item = await context.TodoItems.FirstOrDefaultAsync(todo => todo.Id == todoId && todo.UserId == userId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        item.DeletedAt = now;
        item.DeletedBy = userId;
        var reminders = await context.Reminders.Where(reminder => reminder.TodoItemId == todoId).ToListAsync(cancellationToken);
        foreach (var reminder in reminders)
        {
            reminder.DeletedAt = now;
            reminder.DeletedBy = userId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<TodoItemDto?> FindAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        return (await ListAsync(userId, cancellationToken)).FirstOrDefault(item => item.Id == todoId);
    }

    private static void Apply(TodoItem item, TodoDraft draft)
    {
        item.Title = draft.Title;
        item.Priority = draft.Priority;
        item.DueAt = draft.DueAt;
        item.Recurrence = draft.Recurrence;
    }

    private async Task ReplaceTagsAsync(TodoItem item, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        var tags = await TagDirectory.ResolveAsync(context, names, cancellationToken);
        var wanted = tags.Select(tag => tag.Id).ToHashSet();
        var existing = await context.TodoTags.Where(todoTag => todoTag.TodoItemId == item.Id).ToListAsync(cancellationToken);
        context.TodoTags.RemoveRange(existing.Where(todoTag => !wanted.Contains(todoTag.TagId)));
        foreach (var tag in tags.Where(tag => existing.All(todoTag => todoTag.TagId != tag.Id)))
        {
            context.TodoTags.Add(new TodoTag
            {
                Id = Guid.NewGuid(),
                TodoItemId = item.Id,
                TagId = tag.Id
            });
        }
    }

    private async Task ReplaceReminderAsync(Guid userId, TodoItem item, DateTimeOffset? remindAt, CancellationToken cancellationToken)
    {
        var existing = await context.Reminders.Where(reminder => reminder.TodoItemId == item.Id).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var reminder in existing)
        {
            reminder.DeletedAt = now;
            reminder.DeletedBy = userId;
        }

        if (remindAt is null)
        {
            return;
        }

        context.Reminders.Add(new Reminder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TodoItemId = item.Id,
            RemindAt = remindAt.Value,
            Message = item.Title
        });
    }
}

internal static class TagDirectory
{
    public static async Task<IReadOnlyList<Tag>> ResolveAsync(WorknexaDbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (names.Count == 0)
        {
            return [];
        }

        var lowered = names.Select(name => name.ToLowerInvariant()).ToArray();
        var found = await context.Tags.Where(tag => lowered.Contains(tag.Name.ToLower())).ToListAsync(cancellationToken);
        var resolved = new List<Tag>();
        foreach (var name in names)
        {
            var match = found.FirstOrDefault(tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                match = new Tag { Id = Guid.NewGuid(), Name = name };
                context.Tags.Add(match);
                found.Add(match);
            }

            resolved.Add(match);
        }

        return resolved;
    }
}

internal static class AttachmentFiles
{
    public static async Task<long> SaveAsync(IConfiguration configuration, string storageKey, Stream content, CancellationToken cancellationToken)
    {
        var path = PathFor(configuration, storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return output.Length;
    }

    public static Stream? Open(IConfiguration configuration, string storageKey)
    {
        var path = PathFor(configuration, storageKey);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public static void Delete(IConfiguration configuration, string storageKey)
    {
        var path = PathFor(configuration, storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string PathFor(IConfiguration configuration, string storageKey)
    {
        var root = configuration["Work:AttachmentRoot"];
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "attachments");
        }

        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Attachment path is not valid.");
        }

        return fullPath;
    }
}

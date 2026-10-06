using Worknexa.Domain.Calendar;
using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Knowledge;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Projects;

namespace Worknexa.Domain.Productivity;

public sealed class WorkTask : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;

    public WorkPriority Priority { get; set; } = WorkPriority.Medium;

    public DateTimeOffset? StartAt { get; set; }

    public decimal? EstimatedHours { get; set; }

    public decimal? ActualHours { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? AssigneeId { get; set; }

    public User? Assignee { get; set; }

    public Guid? TeamId { get; set; }

    public Team? Team { get; set; }

    public Guid? ParentTaskId { get; set; }

    public WorkTask? ParentTask { get; set; }

    public Guid? MeetingId { get; set; }

    public Meeting? Meeting { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public int SortOrder { get; set; }

    public ICollection<WorkTask> Subtasks { get; set; } = new List<WorkTask>();

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    public ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();

    public ICollection<TaskDependency> Dependents { get; set; } = new List<TaskDependency>();

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}

public sealed class TaskComment : SoftDeletableEntity
{
    public Guid TaskId { get; set; }

    public WorkTask Task { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
}

public sealed class TaskDependency : AuditableEntity
{
    public Guid TaskId { get; set; }

    public WorkTask Task { get; set; } = null!;

    public Guid DependsOnTaskId { get; set; }

    public WorkTask DependsOnTask { get; set; } = null!;
}

public sealed class Tag : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();

    public ICollection<TodoTag> TodoTags { get; set; } = new List<TodoTag>();

    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
}

public sealed class TaskTag : AuditableEntity
{
    public Guid TaskId { get; set; }

    public WorkTask Task { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}

public sealed class TodoItem : SoftDeletableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public WorkPriority Priority { get; set; } = WorkPriority.Medium;

    public Recurrence Recurrence { get; set; } = Recurrence.None;

    public DateTimeOffset? DueAt { get; set; }

    public int SortOrder { get; set; }

    public ICollection<TodoTag> TodoTags { get; set; } = new List<TodoTag>();
}

public sealed class TodoTag : AuditableEntity
{
    public Guid TodoItemId { get; set; }

    public TodoItem TodoItem { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}

public sealed class Reminder : SoftDeletableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid? TaskId { get; set; }

    public WorkTask? Task { get; set; }

    public Guid? CalendarEventId { get; set; }

    public CalendarEvent? CalendarEvent { get; set; }

    public Guid? TodoItemId { get; set; }

    public TodoItem? TodoItem { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset RemindAt { get; set; }

    public string? Message { get; set; }

    public Recurrence Recurrence { get; set; } = Recurrence.None;

    public int? RecurrenceInterval { get; set; }

    public WorkPriority Priority { get; set; } = WorkPriority.Medium;

    public bool IsCompleted { get; set; }

    public bool IsSent { get; set; }

    public Guid? MeetingId { get; set; }

    public Meeting? Meeting { get; set; }
}

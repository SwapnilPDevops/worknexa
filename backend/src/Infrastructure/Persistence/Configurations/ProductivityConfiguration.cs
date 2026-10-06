using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Productivity;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("Tasks", table =>
        {
            table.HasCheckConstraint("CK_Tasks_Schedule", "\"StartAt\" IS NULL OR \"DueAt\" IS NULL OR \"StartAt\" <= \"DueAt\"");
            table.HasCheckConstraint("CK_Tasks_Hours", "(\"EstimatedHours\" IS NULL OR \"EstimatedHours\" >= 0) AND (\"ActualHours\" IS NULL OR \"ActualHours\" >= 0)");
        });
        builder.ConfigureSoftDelete();
        builder.Property(task => task.Title).HasMaxLength(300).IsRequired();
        builder.Property(task => task.Description).HasColumnType("text");
        builder.Property(task => task.Status).AsStringEnum();
        builder.Property(task => task.Priority).AsStringEnum();
        builder.Property(task => task.EstimatedHours).HasPrecision(8, 2);
        builder.Property(task => task.ActualHours).HasPrecision(8, 2);
        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => task.DueAt);
        builder.HasIndex(task => new { task.ProjectId, task.Status });
        builder.HasIndex(task => new { task.AssigneeId, task.Status });

        builder.HasOne(task => task.Project)
            .WithMany(project => project.Tasks)
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(task => task.Assignee)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(task => task.AssigneeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(task => task.Team)
            .WithMany(team => team.Tasks)
            .HasForeignKey(task => task.TeamId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(task => task.ParentTask)
            .WithMany(task => task.Subtasks)
            .HasForeignKey(task => task.ParentTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(task => task.Meeting)
            .WithMany()
            .HasForeignKey(task => task.MeetingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(task => task.MeetingId);
    }
}

internal sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");
        builder.ConfigureSoftDelete();
        builder.Property(comment => comment.Body).HasColumnType("text").IsRequired();

        builder.HasOne(comment => comment.Task)
            .WithMany(task => task.Comments)
            .HasForeignKey(comment => comment.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies", table =>
        {
            table.HasCheckConstraint("CK_TaskDependencies_NotSelf", "\"TaskId\" <> \"DependsOnTaskId\"");
        });
        builder.ConfigureAudit();
        builder.HasIndex(dependency => new { dependency.TaskId, dependency.DependsOnTaskId }).IsUnique();
        builder.HasIndex(dependency => dependency.DependsOnTaskId);

        builder.HasOne(dependency => dependency.Task)
            .WithMany(task => task.Dependencies)
            .HasForeignKey(dependency => dependency.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dependency => dependency.DependsOnTask)
            .WithMany(task => task.Dependents)
            .HasForeignKey(dependency => dependency.DependsOnTaskId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.ConfigureSoftDelete();
        builder.Property(tag => tag.Name).HasMaxLength(100).IsRequired();
        builder.Property(tag => tag.Color).HasMaxLength(16);
        builder.HasIndex(tag => tag.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> builder)
    {
        builder.ToTable("TaskTags");
        builder.ConfigureAudit();
        builder.HasIndex(taskTag => new { taskTag.TaskId, taskTag.TagId }).IsUnique();

        builder.HasOne(taskTag => taskTag.Task)
            .WithMany(task => task.TaskTags)
            .HasForeignKey(taskTag => taskTag.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(taskTag => taskTag.Tag)
            .WithMany(tag => tag.TaskTags)
            .HasForeignKey(taskTag => taskTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");
        builder.ConfigureSoftDelete();
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Priority).AsStringEnum();
        builder.Property(item => item.Recurrence).AsStringEnum();
        builder.HasIndex(item => new { item.UserId, item.IsCompleted });
        builder.HasIndex(item => new { item.UserId, item.DueAt });

        builder.HasOne(item => item.User)
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TodoTagConfiguration : IEntityTypeConfiguration<TodoTag>
{
    public void Configure(EntityTypeBuilder<TodoTag> builder)
    {
        builder.ToTable("TodoTags");
        builder.ConfigureAudit();
        builder.HasIndex(todoTag => new { todoTag.TodoItemId, todoTag.TagId }).IsUnique();

        builder.HasOne(todoTag => todoTag.TodoItem)
            .WithMany(item => item.TodoTags)
            .HasForeignKey(todoTag => todoTag.TodoItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(todoTag => todoTag.Tag)
            .WithMany(tag => tag.TodoTags)
            .HasForeignKey(todoTag => todoTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders", table =>
        {
            table.HasCheckConstraint("CK_Reminders_Interval", "\"RecurrenceInterval\" IS NULL OR (\"RecurrenceInterval\" BETWEEN 1 AND 366)");
        });
        builder.ConfigureSoftDelete();
        builder.Property(reminder => reminder.Title).HasMaxLength(300).IsRequired().HasDefaultValue("Reminder");
        builder.Property(reminder => reminder.Description).HasColumnType("text");
        builder.Property(reminder => reminder.Message).HasMaxLength(500);
        builder.Property(reminder => reminder.Recurrence).AsStringEnum();
        builder.Property(reminder => reminder.Priority).AsStringEnum();
        builder.Property(reminder => reminder.IsCompleted).HasDefaultValue(false);
        builder.HasIndex(reminder => new { reminder.UserId, reminder.RemindAt });
        builder.HasIndex(reminder => reminder.IsSent);

        builder.HasOne(reminder => reminder.User)
            .WithMany()
            .HasForeignKey(reminder => reminder.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(reminder => reminder.Task)
            .WithMany()
            .HasForeignKey(reminder => reminder.TaskId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(reminder => reminder.CalendarEvent)
            .WithMany()
            .HasForeignKey(reminder => reminder.CalendarEventId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(reminder => reminder.TodoItem)
            .WithMany()
            .HasForeignKey(reminder => reminder.TodoItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(reminder => reminder.Meeting)
            .WithMany()
            .HasForeignKey(reminder => reminder.MeetingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(reminder => reminder.MeetingId);
    }
}

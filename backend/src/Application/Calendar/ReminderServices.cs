using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Calendar;

public interface IReminderStore
{
    Task<IReadOnlyList<ReminderItem>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<ReminderLinks> LinksAsync(Guid userId, CancellationToken cancellationToken);

    Task<ReminderItem> CreateAsync(Guid userId, ReminderDraft draft, CancellationToken cancellationToken);

    Task<ReminderItem?> UpdateAsync(Guid userId, Guid reminderId, ReminderDraft draft, CancellationToken cancellationToken);

    Task<ReminderItem?> SetCompletedAsync(Guid userId, Guid reminderId, bool completed, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderItem>> DueAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task SettleAsync(Guid userId, Guid reminderId, DateTimeOffset remindAt, bool isSent, CancellationToken cancellationToken);
}

public interface INotificationStore
{
    Task AddAsync(Guid userId, NotificationDraft draft, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationItem>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken);

    Task<bool> MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationPreferenceItem>> PreferencesAsync(Guid userId, CancellationToken cancellationToken);

    Task SetPreferenceAsync(Guid userId, NotificationType type, bool enabled, CancellationToken cancellationToken);

    Task CollectSignalsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IReminderService
{
    Task<IReadOnlyList<ReminderItem>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<ReminderLinks> LinksAsync(Guid userId, CancellationToken cancellationToken);

    Task<ReminderItem> CreateAsync(Guid userId, ReminderWrite request, CancellationToken cancellationToken);

    Task<ReminderItem> UpdateAsync(Guid userId, Guid reminderId, ReminderWrite request, CancellationToken cancellationToken);

    Task<ReminderItem> CompleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken);

    Task<ReminderItem> ReopenAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken);

    Task<int> DispatchDueAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationItem>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationPreferenceItem>> PreferencesAsync(Guid userId, CancellationToken cancellationToken);

    Task SetPreferenceAsync(Guid userId, NotificationPreferenceWrite request, CancellationToken cancellationToken);
}

public sealed class ReminderService(IReminderStore store, INotificationStore notifications) : IReminderService
{
    public async Task<IReadOnlyList<ReminderItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await DispatchDueAsync(userId, DateTimeOffset.UtcNow, cancellationToken);
        return await store.ListAsync(userId, cancellationToken);
    }

    public Task<ReminderLinks> LinksAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.LinksAsync(userId, cancellationToken);
    }

    public Task<ReminderItem> CreateAsync(Guid userId, ReminderWrite request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, Draft(request), cancellationToken);
    }

    public async Task<ReminderItem> UpdateAsync(Guid userId, Guid reminderId, ReminderWrite request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, reminderId, Draft(request), cancellationToken)
            ?? throw new NotFoundException("Reminder was not found.");
    }

    public async Task<ReminderItem> CompleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken)
    {
        return await store.SetCompletedAsync(userId, reminderId, true, cancellationToken)
            ?? throw new NotFoundException("Reminder was not found.");
    }

    public async Task<ReminderItem> ReopenAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken)
    {
        return await store.SetCompletedAsync(userId, reminderId, false, cancellationToken)
            ?? throw new NotFoundException("Reminder was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, reminderId, cancellationToken))
        {
            throw new NotFoundException("Reminder was not found.");
        }
    }

    public async Task<int> DispatchDueAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await store.DueAsync(userId, now, cancellationToken);
        foreach (var reminder in due)
        {
            var recurrence = Enum.TryParse<Recurrence>(reminder.Recurrence, out var parsed) ? parsed : Recurrence.None;
            var step = NextStep(reminder.RemindAt, recurrence, reminder.RecurrenceInterval, now);
            await notifications.AddAsync(userId, new NotificationDraft(
                reminder.Title,
                string.IsNullOrWhiteSpace(reminder.Description) ? "A reminder is due." : Trim(reminder.Description, 2000),
                Link(reminder),
                reminder.Id,
                NotificationType.ReminderDue,
                reminder.Id), cancellationToken);
            await store.SettleAsync(userId, reminder.Id, step.RemindAt, step.IsSent, cancellationToken);
        }

        return due.Count;
    }

    internal static (DateTimeOffset RemindAt, bool IsSent) NextStep(DateTimeOffset remindAt, Recurrence recurrence, int? interval, DateTimeOffset now)
    {
        if (!DueSchedule.CanAdvance(recurrence, interval))
        {
            return (remindAt, true);
        }

        var next = remindAt;
        for (var guard = 0; guard < 32 && next <= now; guard++)
        {
            var advanced = DueSchedule.Advance(next, recurrence, interval);
            if (advanced <= next)
            {
                return (next, true);
            }

            next = advanced;
        }

        return next <= now ? (next, true) : (next, false);
    }

    private static ReminderDraft Draft(ReminderWrite request)
    {
        var recurrence = TaskService.ParseRequired<Recurrence>(request.Recurrence, "recurrence");
        return new ReminderDraft(
            request.Title.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.RemindAt,
            recurrence,
            recurrence == Recurrence.Custom ? request.RecurrenceInterval : null,
            TaskService.ParseRequired<WorkPriority>(request.Priority, "priority"),
            request.TaskId,
            request.MeetingId);
    }

    private static string Link(ReminderItem reminder)
    {
        if (reminder.TaskId is not null)
        {
            return "/tasks";
        }

        if (reminder.MeetingId is not null || reminder.CalendarEventId is not null)
        {
            return "/calendar";
        }

        return "/reminders";
    }

    private static string Trim(string value, int length)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= length ? trimmed : trimmed[..length];
    }
}

public sealed class NotificationService(IReminderService reminders, INotificationStore store) : INotificationService
{
    public async Task<IReadOnlyList<NotificationItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await reminders.DispatchDueAsync(userId, now, cancellationToken);
        await store.CollectSignalsAsync(userId, now, cancellationToken);
        return await store.ListAsync(userId, cancellationToken);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        if (!await store.MarkReadAsync(userId, notificationId, DateTimeOffset.UtcNow, cancellationToken))
        {
            throw new NotFoundException("Notification was not found.");
        }
    }

    public async Task MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        if (!await store.MarkUnreadAsync(userId, notificationId, cancellationToken))
        {
            throw new NotFoundException("Notification was not found.");
        }
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.MarkAllReadAsync(userId, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<IReadOnlyList<NotificationPreferenceItem>> PreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.PreferencesAsync(userId, cancellationToken);
    }

    public Task SetPreferenceAsync(Guid userId, NotificationPreferenceWrite request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, out var type))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("type", "Choose a notification type.")]);
        }

        return store.SetPreferenceAsync(userId, type, request.Enabled, cancellationToken);
    }
}

public sealed class ReminderWriteValidator : AbstractValidator<ReminderWrite>
{
    public ReminderWriteValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Description).MaximumLength(8000);
        RuleFor(request => request.Recurrence).NotEmpty();
        RuleFor(request => request.Priority).NotEmpty();
        RuleFor(request => request.RecurrenceInterval)
            .NotNull()
            .InclusiveBetween(1, 366)
            .When(request => request.Recurrence == nameof(Recurrence.Custom));
    }
}

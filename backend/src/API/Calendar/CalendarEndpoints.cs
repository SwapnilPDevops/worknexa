using Worknexa.Api.Contracts;
using Worknexa.Api.Dashboard;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Calendar;
using Worknexa.Application.Platform;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Calendar;

public static class CalendarEndpoints
{
    public static RouteGroupBuilder MapWorknexaCalendar(this RouteGroupBuilder api)
    {
        var calendar = api.MapGroup("/calendar").WithTags("Calendar");
        calendar.MapGet("/", async (
            DateTimeOffset from,
            DateTimeOffset to,
            ICalendarService service,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await PermissionsAsync(httpContext, userId, cancellationToken);
            var entries = await service.ListAsync(
                new CalendarRead(
                    userId,
                    from,
                    to,
                    permissions.Contains(PermissionCatalog.TasksRead),
                    permissions.Contains(PermissionCatalog.MeetingsRead)),
                cancellationToken);
            return ApiResults.Success(entries);
        }).RequireAuthorization(PermissionCatalog.CalendarRead);

        calendar.MapGet("/events/{eventId:guid}", async (Guid eventId, ICalendarService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), eventId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.CalendarRead);

        calendar.MapPost("/events", async (CalendarEventWrite request, ICalendarService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.CalendarManage);

        calendar.MapPut("/events/{eventId:guid}", async (Guid eventId, CalendarEventWrite request, ICalendarService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), eventId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.CalendarManage);

        calendar.MapDelete("/events/{eventId:guid}", async (Guid eventId, ICalendarService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), eventId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.CalendarManage);

        var reminders = api.MapGroup("/reminders").WithTags("Reminders");
        reminders.MapGet("/", async (IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksRead);

        reminders.MapGet("/links", async (IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.LinksAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksRead);

        reminders.MapPost("/", async (ReminderWrite request, IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        reminders.MapPut("/{reminderId:guid}", async (Guid reminderId, ReminderWrite request, IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), reminderId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        reminders.MapPost("/{reminderId:guid}/complete", async (Guid reminderId, IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CompleteAsync(RequireUser(currentUser), reminderId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        reminders.MapPost("/{reminderId:guid}/reopen", async (Guid reminderId, IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ReopenAsync(RequireUser(currentUser), reminderId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        reminders.MapDelete("/{reminderId:guid}", async (Guid reminderId, IReminderService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), reminderId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.TasksManage);

        var notifications = api.MapGroup("/notifications").WithTags("Notifications");
        notifications.MapGet("/", async (INotificationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotificationsRead);

        notifications.MapGet("/preferences", async (INotificationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.PreferencesAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotificationsRead);

        notifications.MapPut("/preferences", async (NotificationPreferenceWrite request, INotificationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.SetPreferenceAsync(RequireUser(currentUser), request, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.NotificationsRead);

        notifications.MapPost("/{notificationId:guid}/read", async (Guid notificationId, INotificationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.MarkReadAsync(RequireUser(currentUser), notificationId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.NotificationsRead);

        notifications.MapPost("/{notificationId:guid}/unread", async (Guid notificationId, INotificationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.MarkUnreadAsync(RequireUser(currentUser), notificationId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.NotificationsRead);

        notifications.MapPost("/read", async (INotificationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.MarkAllReadAsync(RequireUser(currentUser), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.NotificationsRead);

        api.MapGet("/activity", async (IActivityService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotificationsRead);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }

    private static async Task<IReadOnlyList<string>> PermissionsAsync(HttpContext httpContext, Guid userId, CancellationToken cancellationToken)
    {
        if (httpContext.Items[DashboardEndpoints.AccountItemKey] is AccountRecord cached)
        {
            return cached.Permissions;
        }

        var accounts = httpContext.RequestServices.GetRequiredService<IAccountStore>();
        var account = await accounts.FindByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedAccessException();
        return account.Permissions;
    }
}

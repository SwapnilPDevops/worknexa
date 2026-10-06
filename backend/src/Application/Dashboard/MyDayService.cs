using FluentValidation;
using FluentValidation.Results;
using Worknexa.Application.Common;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Dashboard;

public interface IMyDayService
{
    Task<MyDayResponse> GetAsync(Guid userId, IReadOnlyList<string> permissions, DateOnly date, int offsetMinutes, CancellationToken cancellationToken);

    Task CaptureAsync(Guid userId, IReadOnlyList<string> permissions, QuickCaptureRequest request, CancellationToken cancellationToken);
}

public sealed class MyDayService(IMyDayQuery query, IQuickCaptureStore captures) : IMyDayService
{
    public async Task<MyDayResponse> GetAsync(
        Guid userId,
        IReadOnlyList<string> permissions,
        DateOnly date,
        int offsetMinutes,
        CancellationToken cancellationToken)
    {
        var window = Window(date, offsetMinutes);
        var snapshot = await query.LoadAsync(userId, window, MyDayScope.From(permissions), cancellationToken);
        return MyDayComposer.Compose(snapshot, window, DateTimeOffset.UtcNow);
    }

    public Task CaptureAsync(Guid userId, IReadOnlyList<string> permissions, QuickCaptureRequest request, CancellationToken cancellationToken)
    {
        var required = RequiredPermission(request.Kind);
        if (!permissions.Contains(required))
        {
            throw new ForbiddenException("You do not have permission to do that.");
        }

        var window = Window(request.Date, request.OffsetMinutes);
        var now = DateTimeOffset.UtcNow;
        var evening = window.DayStart.AddHours(17);
        var dueAt = evening > now ? evening : window.DayEnd.AddMinutes(-1);
        return captures.CaptureAsync(userId, request, dueAt, now, cancellationToken);
    }

    private static MyDayWindow Window(DateOnly date, int offsetMinutes)
    {
        if (offsetMinutes is < -840 or > 840)
        {
            throw new ValidationException([new ValidationFailure("offsetMinutes", "Offset is out of range.")]);
        }

        return MyDayWindow.Create(date, offsetMinutes);
    }

    private static string RequiredPermission(string kind)
    {
        return kind switch
        {
            QuickCaptureKinds.Task or QuickCaptureKinds.Todo or QuickCaptureKinds.Reminder or QuickCaptureKinds.TeamTask => PermissionCatalog.TasksManage,
            QuickCaptureKinds.Meeting => PermissionCatalog.MeetingsManage,
            QuickCaptureKinds.Note => PermissionCatalog.NotesManage,
            QuickCaptureKinds.LearningNote => PermissionCatalog.LearningManage,
            _ => PermissionCatalog.TasksManage
        };
    }
}

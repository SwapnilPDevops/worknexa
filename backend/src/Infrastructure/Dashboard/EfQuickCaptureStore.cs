using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Dashboard;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Knowledge;
using Worknexa.Domain.Learning;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Productivity;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Dashboard;

public sealed class EfQuickCaptureStore(WorknexaDbContext context) : IQuickCaptureStore
{
    public async Task CaptureAsync(Guid userId, QuickCaptureRequest request, DateTimeOffset dueAt, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        switch (request.Kind)
        {
            case QuickCaptureKinds.Task:
                context.Tasks.Add(NewTask(userId, title, notes, dueAt, teamId: null));
                break;
            case QuickCaptureKinds.Todo:
                context.TodoItems.Add(new TodoItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    DueAt = dueAt
                });
                break;
            case QuickCaptureKinds.Meeting:
                AddMeeting(userId, title, notes, now);
                break;
            case QuickCaptureKinds.Reminder:
                context.Reminders.Add(new Reminder
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Message = title,
                    RemindAt = now.AddHours(1)
                });
                break;
            case QuickCaptureKinds.Note:
                context.Notes.Add(new Note
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Body = notes ?? title
                });
                break;
            case QuickCaptureKinds.TeamTask:
                await AddTeamTaskAsync(userId, title, notes, dueAt, cancellationToken);
                break;
            case QuickCaptureKinds.LearningNote:
                await AddLearningNoteAsync(userId, title, notes, cancellationToken);
                break;
            default:
                throw new ConflictException("Choose a supported quick action.");
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private void AddMeeting(Guid userId, string title, string? notes, DateTimeOffset now)
    {
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute < 30 ? 30 : 0, 0, TimeSpan.Zero).AddHours(now.Minute < 30 ? 0 : 1);
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = notes,
            MeetingType = MeetingType.Team,
            OrganizerId = userId,
            StartAt = start,
            EndAt = start.AddMinutes(30)
        };
        meeting.Participants.Add(new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResponseStatus = ParticipantResponse.Accepted
        });
        context.Meetings.Add(meeting);
    }

    private async Task AddTeamTaskAsync(Guid userId, string title, string? notes, DateTimeOffset dueAt, CancellationToken cancellationToken)
    {
        var teamId = await context.TeamMembers.AsNoTracking()
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.CreatedAt)
            .Select(member => (Guid?)member.TeamId)
            .FirstOrDefaultAsync(cancellationToken);
        if (teamId is null)
        {
            throw new ConflictException("Join a team before assigning a team task.");
        }

        context.Tasks.Add(NewTask(userId, title, notes, dueAt, teamId));
    }

    private async Task AddLearningNoteAsync(Guid userId, string title, string? notes, CancellationToken cancellationToken)
    {
        var topicId = await context.LearningTopics
            .Where(topic => topic.UserId == userId && topic.Status != LearningStatus.Completed)
            .OrderBy(topic => topic.Status == LearningStatus.Learning || topic.Status == LearningStatus.Practicing ? 0 : 1)
            .ThenByDescending(topic => topic.UpdatedAt)
            .Select(topic => (Guid?)topic.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (topicId is null)
        {
            var topic = new LearningTopic
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Status = LearningStatus.Learning,
                Priority = WorkPriority.Medium,
                Progress = 25
            };
            context.LearningTopics.Add(topic);
            topicId = topic.Id;
        }

        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = notes ?? title,
            LearningTopicId = topicId.Value,
            Category = "Learning"
        };
        context.Notes.Add(note);
        context.Entry(note).State = EntityState.Added;
    }

    private static WorkTask NewTask(Guid userId, string title, string? notes, DateTimeOffset dueAt, Guid? teamId)
    {
        return new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = notes,
            AssigneeId = teamId is null ? userId : null,
            TeamId = teamId,
            DueAt = dueAt,
            Status = WorkTaskStatus.Todo,
            Priority = WorkPriority.Medium
        };
    }
}

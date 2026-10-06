using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Worknexa.Application.Analytics;
using Worknexa.Application.Assistant;
using Worknexa.Application.Assistant.Tools;
using Worknexa.Application.Auth;
using Worknexa.Application.Calendar;
using Worknexa.Application.Dashboard;
using Worknexa.Application.Configuration;
using Worknexa.Application.Integrations;
using Worknexa.Application.Integrations.Calendar;
using Worknexa.Application.Knowledge;
using Worknexa.Application.Learning;
using Worknexa.Application.Leaves;
using Worknexa.Application.Meetings;
using Worknexa.Application.Organization;
using Worknexa.Application.Platform;
using Worknexa.Application.Projects;
using Worknexa.Application.Work;

namespace Worknexa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<PaginationSettings>()
            .BindConfiguration(PaginationSettings.SectionName)
            .Validate(
                settings => settings.DefaultPageSize > 0 && settings.MaxPageSize >= settings.DefaultPageSize,
                "Pagination settings are invalid.")
            .ValidateOnStart();

        services.AddValidatorsFromAssembly(ApplicationAssembly.Assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMyDayService, MyDayService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<ICalendarSource, LocalCalendarSource>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IMeetingNoteService, MeetingNoteService>();
        services.AddScoped<IStandupService, StandupService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<ILearningService, LearningService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddAssistant();
        services.AddIntegrations();
        return services;
    }

    private static void AddIntegrations(this IServiceCollection services)
    {
        services.AddOptions<IntegrationSettings>().BindConfiguration(IntegrationSettings.SectionName);
        services.AddScoped<IntegrationProviders>();
        services.AddScoped<CalendarSyncEngine>();
        services.AddScoped<IIntegrationService, IntegrationService>();
    }

    private static void AddAssistant(this IServiceCollection services)
    {
        services.AddOptions<AssistantSettings>().BindConfiguration(AssistantSettings.SectionName);
        services.AddSingleton<IAssistantPrompts, EmbeddedAssistantPrompts>();
        services.AddScoped<AssistantLookup>();
        services.AddScoped<IAssistantTool, CreateTaskTool>();
        services.AddScoped<IAssistantTool, UpdateTaskTool>();
        services.AddScoped<IAssistantTool, SearchTasksTool>();
        services.AddScoped<IAssistantTool, GetMyDayTool>();
        services.AddScoped<IAssistantTool, GetCalendarTool>();
        services.AddScoped<IAssistantTool, CreateReminderTool>();
        services.AddScoped<IAssistantTool, SearchProjectsTool>();
        services.AddScoped<IAssistantTool, GetProjectStatusTool>();
        services.AddScoped<IAssistantTool, SearchTeamTool>();
        services.AddScoped<IAssistantTool, GetTeamWorkloadTool>();
        services.AddScoped<IAssistantTool, SearchNotesTool>();
        services.AddScoped<IAssistantTool, CreateMeetingNoteTool>();
        services.AddScoped<IAssistantTool, CreateLearningNoteTool>();
        services.AddScoped<IAssistantTool, GetLearningProgressTool>();
        services.AddScoped<AssistantToolRegistry>();
        services.AddScoped<IAssistantService, AssistantService>();
    }
}

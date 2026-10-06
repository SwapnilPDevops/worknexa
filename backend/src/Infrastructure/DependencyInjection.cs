using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Analytics;
using Worknexa.Application.Assistant;
using Worknexa.Infrastructure.Assistant;
using Worknexa.Application.Dashboard;
using Worknexa.Infrastructure.Health;
using Worknexa.Application.Calendar;
using Worknexa.Application.Integrations;
using Worknexa.Application.Integrations.Calendar;
using Worknexa.Infrastructure.Integrations;
using Worknexa.Infrastructure.Integrations.Outlook;
using Worknexa.Application.Knowledge;
using Worknexa.Application.Learning;
using Worknexa.Application.Leaves;
using Worknexa.Application.Meetings;
using Worknexa.Application.Organization;
using Worknexa.Application.Platform;
using Worknexa.Application.Projects;
using Worknexa.Application.Search;
using Worknexa.Application.Work;
using Worknexa.Infrastructure.Analytics;
using Worknexa.Infrastructure.Calendar;
using Worknexa.Infrastructure.Dashboard;
using Worknexa.Infrastructure.Knowledge;
using Worknexa.Infrastructure.Learning;
using Worknexa.Infrastructure.Leaves;
using Worknexa.Infrastructure.Meetings;
using Worknexa.Infrastructure.Organization;
using Worknexa.Infrastructure.Platform;
using Worknexa.Infrastructure.Projects;
using Worknexa.Infrastructure.Search;
using Worknexa.Infrastructure.Work;
using Worknexa.Infrastructure.Identity;
using Worknexa.Infrastructure.Persistence;
using Worknexa.Infrastructure.Persistence.Auditing;

namespace Worknexa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Worknexa")
            ?? throw new InvalidOperationException("Connection string 'Worknexa' is not configured.");

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<WorknexaDbContext>((serviceProvider, options) =>
        {
            options.UseWorknexaPostgres(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IAccountStore, EfAccountStore>();
        services.AddScoped<IMyDayQuery, EfMyDayQuery>();
        services.AddScoped<IQuickCaptureStore, EfQuickCaptureStore>();
        services.AddScoped<ITaskStore, EfTaskStore>();
        services.AddScoped<ITodoStore, EfTodoStore>();
        services.AddScoped<ICalendarStore, EfCalendarStore>();
        services.AddScoped<IReminderStore, EfReminderStore>();
        services.AddScoped<INotificationStore, EfNotificationStore>();
        services.AddScoped<IActivityStore, EfActivityStore>();
        services.AddScoped<IMeetingStore, EfMeetingStore>();
        services.AddScoped<IMeetingNoteStore, EfMeetingNoteStore>();
        services.AddScoped<IStandupStore, EfStandupStore>();
        services.AddScoped<INoteStore, EfNoteStore>();
        services.AddScoped<IProjectStore, EfProjectStore>();
        services.AddScoped<ITeamStore, EfTeamStore>();
        services.AddScoped<ILeaveStore, EfLeaveStore>();
        services.AddScoped<ILearningStore, EfLearningStore>();
        services.AddScoped<IWorkspaceSearch, EfWorkspaceSearch>();
        services.AddScoped<IAnalyticsStore, EfAnalyticsStore>();
        services.AddAssistantModel(configuration);
        services.AddIntegrationProviders();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                failureStatus: null,
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<MigrationsHealthCheck>(
                "migrations",
                failureStatus: null,
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5));
        services.AddHostedService<SeededCredentialsWarning>();

        return services;
    }

    /// <summary>Assistant:Provider picks the model adapter. Add a case here to support another provider.</summary>
    private static void AddAssistantModel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAssistantActionStore, InMemoryAssistantActionStore>();
        services.AddOptions<OpenAiSettings>().BindConfiguration(OpenAiSettings.SectionName);

        var provider = configuration["Assistant:Provider"] ?? "OpenAI";
        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var timeout = configuration.GetValue<int?>($"{OpenAiSettings.SectionName}:TimeoutSeconds") ?? 60;
            services.AddHttpClient<IAssistantModel, OpenAiAssistantModel>(client => client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeout, 5, 300)));
            return;
        }

        services.AddSingleton<IAssistantModel, DisabledAssistantModel>();
    }

    /// <summary>One registration per implemented provider. Planned providers stay in the catalog until they are added here.</summary>
    private static void AddIntegrationProviders(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationStore, EfIntegrationStore>();
        services.AddScoped<ICalendarSyncStore, EfCalendarSyncStore>();

        services.AddOptions<OutlookCalendarSettings>().BindConfiguration(OutlookCalendarSettings.SectionName);
        services.AddHttpClient<OutlookCalendarProvider>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IIntegrationProvider>(provider => provider.GetRequiredService<OutlookCalendarProvider>());
    }
}

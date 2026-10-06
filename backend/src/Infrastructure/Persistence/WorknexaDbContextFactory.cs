using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Worknexa.Infrastructure.Persistence.Auditing;

namespace Worknexa.Infrastructure.Persistence;

public sealed class WorknexaDbContextFactory : IDesignTimeDbContextFactory<WorknexaDbContext>
{
    public WorknexaDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.GetFullPath(Path.Combine(basePath, "src", "API"));
        if (Directory.Exists(apiPath))
        {
            basePath = apiPath;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // No fallback: a migration run with a missing setting must fail instead of targeting some other database.
        var connectionString = configuration.GetConnectionString("Worknexa")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Worknexa (or appsettings.Development.json locally) before running migrations.");

        var builder = new DbContextOptionsBuilder<WorknexaDbContext>();
        builder.UseWorknexaPostgres(connectionString);
        builder.AddInterceptors(new AuditSaveChangesInterceptor());
        return new WorknexaDbContext(builder.Options);
    }
}

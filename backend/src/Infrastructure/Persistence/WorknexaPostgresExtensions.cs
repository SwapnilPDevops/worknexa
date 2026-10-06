using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Worknexa.Infrastructure.Persistence;

public static class WorknexaPostgresExtensions
{
    public static DbContextOptionsBuilder UseWorknexaPostgres(this DbContextOptionsBuilder builder, string connectionString)
    {
        builder.UseNpgsql(connectionString, postgres =>
        {
            postgres.MigrationsHistoryTable("__EFMigrationsHistory", "worknexa");
            postgres.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
        builder.ConfigureWarnings(warnings =>
            warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        return builder;
    }

    public static DbContextOptionsBuilder<TContext> UseWorknexaPostgres<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        string connectionString)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)builder).UseWorknexaPostgres(connectionString);
        return builder;
    }
}

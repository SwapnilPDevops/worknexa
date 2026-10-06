using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Integrations;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class IntegrationConnectionConfiguration : IEntityTypeConfiguration<IntegrationConnection>
{
    public void Configure(EntityTypeBuilder<IntegrationConnection> builder)
    {
        builder.ToTable("IntegrationConnections");
        builder.ConfigureAudit();
        builder.Property(connection => connection.Provider).HasMaxLength(64).IsRequired();
        builder.Property(connection => connection.Category).AsStringEnum();
        builder.Property(connection => connection.Status).AsStringEnum();
        builder.Property(connection => connection.Direction).AsStringEnum();
        builder.Property(connection => connection.ExternalAccountId).HasMaxLength(200);
        builder.Property(connection => connection.ExternalAccountName).HasMaxLength(200);
        builder.Property(connection => connection.ExternalContainerId).HasMaxLength(500);
        builder.Property(connection => connection.ExternalContainerName).HasMaxLength(200);
        builder.Property(connection => connection.TimeZone).HasMaxLength(64);
        builder.Property(connection => connection.AccessTokenProtected).HasColumnType("text");
        builder.Property(connection => connection.RefreshTokenProtected).HasColumnType("text");
        builder.Property(connection => connection.SyncCursor).HasColumnType("text");
        builder.Property(connection => connection.LastError).HasMaxLength(1000);
        builder.HasIndex(connection => new { connection.UserId, connection.Provider }).IsUnique();
        builder.HasIndex(connection => new { connection.SyncEnabled, connection.Status, connection.NextSyncAt });

        builder.HasOne(connection => connection.User)
            .WithMany()
            .HasForeignKey(connection => connection.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExternalLinkConfiguration : IEntityTypeConfiguration<ExternalLink>
{
    public void Configure(EntityTypeBuilder<ExternalLink> builder)
    {
        builder.ToTable("ExternalLinks");
        builder.ConfigureAudit();
        builder.Property(link => link.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(link => link.ExternalId).HasMaxLength(500).IsRequired();
        builder.Property(link => link.ExternalVersion).HasMaxLength(200);
        builder.Property(link => link.SyncedHash).HasMaxLength(64).IsRequired();
        builder.Property(link => link.Origin).AsStringEnum();
        builder.HasIndex(link => new { link.ConnectionId, link.EntityType, link.ExternalId }).IsUnique();
        builder.HasIndex(link => new { link.ConnectionId, link.EntityType, link.LocalId }).IsUnique();
        builder.HasIndex(link => new { link.EntityType, link.LocalId });

        builder.HasOne(link => link.Connection)
            .WithMany(connection => connection.Links)
            .HasForeignKey(link => link.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SyncRunConfiguration : IEntityTypeConfiguration<SyncRun>
{
    public void Configure(EntityTypeBuilder<SyncRun> builder)
    {
        builder.ToTable("SyncRuns");
        builder.ConfigureAudit();
        builder.Property(run => run.Trigger).AsStringEnum();
        builder.Property(run => run.Direction).AsStringEnum();
        builder.Property(run => run.Status).AsStringEnum();
        builder.Property(run => run.Error).HasMaxLength(1000);
        builder.HasIndex(run => new { run.ConnectionId, run.StartedAt });

        builder.HasOne(run => run.Connection)
            .WithMany(connection => connection.Runs)
            .HasForeignKey(run => run.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SyncFailureConfiguration : IEntityTypeConfiguration<SyncFailure>
{
    public void Configure(EntityTypeBuilder<SyncFailure> builder)
    {
        builder.ToTable("SyncFailures");
        builder.ConfigureAudit();
        builder.Property(failure => failure.Operation).HasMaxLength(32).IsRequired();
        builder.Property(failure => failure.ExternalId).HasMaxLength(500);
        builder.Property(failure => failure.Title).HasMaxLength(300);
        builder.Property(failure => failure.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(failure => failure.RunId);

        builder.HasOne(failure => failure.Run)
            .WithMany(run => run.Failures)
            .HasForeignKey(failure => failure.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

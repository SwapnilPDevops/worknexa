using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Platform;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.ConfigureSoftDelete();
        builder.Property(notification => notification.Title).HasMaxLength(300).IsRequired();
        builder.Property(notification => notification.Body).HasMaxLength(2000).IsRequired();
        builder.Property(notification => notification.Link).HasMaxLength(2048);
        builder.Property(notification => notification.Type).AsStringEnum();
        builder.HasIndex(notification => new { notification.UserId, notification.IsRead });
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt });
        builder.HasIndex(notification => notification.ReminderId);
        builder.HasIndex(notification => new { notification.UserId, notification.Type, notification.SourceId });

        builder.HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.ConfigureAudit();
        builder.Property(log => log.EntityName).HasMaxLength(64).IsRequired();
        builder.Property(log => log.Action).HasMaxLength(64).IsRequired();
        builder.Property(log => log.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(log => log.Metadata).HasMaxLength(4000);
        builder.HasIndex(log => new { log.EntityName, log.EntityId });
        builder.HasIndex(log => log.CreatedAt);
    }
}

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.ConfigureAudit();
        builder.Property(preference => preference.Type).AsStringEnum();
        builder.HasIndex(preference => new { preference.UserId, preference.Type }).IsUnique();

        builder.HasOne(preference => preference.User)
            .WithMany()
            .HasForeignKey(preference => preference.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.ConfigureSoftDelete();
        builder.Property(attachment => attachment.EntityName).HasMaxLength(64).IsRequired();
        builder.Property(attachment => attachment.FileName).HasMaxLength(260).IsRequired();
        builder.Property(attachment => attachment.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(attachment => attachment.StorageKey).HasMaxLength(512).IsRequired();
        builder.HasIndex(attachment => attachment.StorageKey).IsUnique();
        builder.HasIndex(attachment => new { attachment.EntityName, attachment.EntityId });
    }
}

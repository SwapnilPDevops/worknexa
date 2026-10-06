using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Calendar;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("CalendarEvents", table =>
        {
            table.HasCheckConstraint("CK_CalendarEvents_TimeRange", "\"EndAt\" >= \"StartAt\"");
            table.HasCheckConstraint("CK_CalendarEvents_Interval", "\"RecurrenceInterval\" IS NULL OR (\"RecurrenceInterval\" BETWEEN 1 AND 366)");
        });
        builder.ConfigureSoftDelete();
        builder.Property(calendarEvent => calendarEvent.Title).HasMaxLength(300).IsRequired();
        builder.Property(calendarEvent => calendarEvent.Description).HasColumnType("text");
        builder.Property(calendarEvent => calendarEvent.Location).HasMaxLength(300);
        builder.Property(calendarEvent => calendarEvent.MeetingLink).HasMaxLength(500);
        builder.Property(calendarEvent => calendarEvent.Kind).AsStringEnum();
        builder.Property(calendarEvent => calendarEvent.Recurrence).AsStringEnum();
        builder.HasIndex(calendarEvent => calendarEvent.ProjectId);
        builder.HasIndex(calendarEvent => calendarEvent.StartAt);
        builder.HasIndex(calendarEvent => new { calendarEvent.OrganizerId, calendarEvent.StartAt });

        builder.HasOne(calendarEvent => calendarEvent.Organizer)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.OrganizerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(calendarEvent => calendarEvent.Team)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.TeamId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(calendarEvent => calendarEvent.Project)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.ToTable("EventParticipants");
        builder.ConfigureAudit();
        builder.Property(participant => participant.ResponseStatus).AsStringEnum();
        builder.HasIndex(participant => new { participant.CalendarEventId, participant.UserId }).IsUnique();

        builder.HasOne(participant => participant.CalendarEvent)
            .WithMany(calendarEvent => calendarEvent.Participants)
            .HasForeignKey(participant => participant.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(participant => participant.User)
            .WithMany()
            .HasForeignKey(participant => participant.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

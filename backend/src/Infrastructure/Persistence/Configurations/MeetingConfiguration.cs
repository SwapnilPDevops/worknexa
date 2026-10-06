using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Meetings;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings", table =>
        {
            table.HasCheckConstraint("CK_Meetings_TimeRange", "\"EndAt\" >= \"StartAt\"");
        });
        builder.ConfigureSoftDelete();
        builder.Property(meeting => meeting.Title).HasMaxLength(300).IsRequired();
        builder.Property(meeting => meeting.Description).HasColumnType("text");
        builder.Property(meeting => meeting.Location).HasMaxLength(300);
        builder.Property(meeting => meeting.MeetingLink).HasMaxLength(500);
        builder.Property(meeting => meeting.MeetingType).AsStringEnum();
        builder.Property(meeting => meeting.Agenda).HasColumnType("text");
        builder.Property(meeting => meeting.NotesText).HasColumnType("text");
        builder.Property(meeting => meeting.Decisions).HasColumnType("text");
        builder.Property(meeting => meeting.FollowUps).HasColumnType("text");
        builder.HasIndex(meeting => meeting.MeetingType);
        builder.HasIndex(meeting => meeting.StartAt);
        builder.HasIndex(meeting => new { meeting.OrganizerId, meeting.StartAt });
        builder.HasIndex(meeting => meeting.ProjectId);
        builder.HasIndex(meeting => meeting.TeamId);

        builder.HasOne(meeting => meeting.Organizer)
            .WithMany()
            .HasForeignKey(meeting => meeting.OrganizerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(meeting => meeting.Team)
            .WithMany()
            .HasForeignKey(meeting => meeting.TeamId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(meeting => meeting.Project)
            .WithMany()
            .HasForeignKey(meeting => meeting.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(meeting => meeting.CalendarEvent)
            .WithMany()
            .HasForeignKey(meeting => meeting.CalendarEventId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.ToTable("MeetingParticipants");
        builder.ConfigureAudit();
        builder.Property(participant => participant.ResponseStatus).AsStringEnum();
        builder.HasIndex(participant => new { participant.MeetingId, participant.UserId }).IsUnique();

        builder.HasOne(participant => participant.Meeting)
            .WithMany(meeting => meeting.Participants)
            .HasForeignKey(participant => participant.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(participant => participant.User)
            .WithMany(user => user.MeetingParticipations)
            .HasForeignKey(participant => participant.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class MeetingNoteConfiguration : IEntityTypeConfiguration<MeetingNote>
{
    public void Configure(EntityTypeBuilder<MeetingNote> builder)
    {
        builder.ToTable("MeetingNotes");
        builder.ConfigureSoftDelete();
        builder.Property(note => note.Body).HasColumnType("text").IsRequired();

        builder.HasOne(note => note.Meeting)
            .WithMany(meeting => meeting.Notes)
            .HasForeignKey(note => note.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MeetingActionItemConfiguration : IEntityTypeConfiguration<MeetingActionItem>
{
    public void Configure(EntityTypeBuilder<MeetingActionItem> builder)
    {
        builder.ToTable("MeetingActionItems");
        builder.ConfigureSoftDelete();
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Description).HasColumnType("text");
        builder.HasIndex(item => item.AssigneeId);
        builder.HasIndex(item => item.TaskId);

        builder.HasOne(item => item.Meeting)
            .WithMany(meeting => meeting.ActionItems)
            .HasForeignKey(item => item.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Assignee)
            .WithMany()
            .HasForeignKey(item => item.AssigneeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(item => item.Task)
            .WithMany()
            .HasForeignKey(item => item.TaskId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class StandupConfiguration : IEntityTypeConfiguration<Standup>
{
    public void Configure(EntityTypeBuilder<Standup> builder)
    {
        builder.ToTable("Standups", table =>
        {
            table.HasCheckConstraint("CK_Standups_Scope", "(\"TeamId\" IS NOT NULL AND \"OwnerId\" IS NULL) OR (\"TeamId\" IS NULL AND \"OwnerId\" IS NOT NULL)");
        });
        builder.ConfigureSoftDelete();
        builder.HasIndex(standup => new { standup.TeamId, standup.StandupDate })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"TeamId\" IS NOT NULL");
        builder.HasIndex(standup => new { standup.OwnerId, standup.StandupDate })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"OwnerId\" IS NOT NULL");
        builder.HasIndex(standup => standup.MeetingId);

        builder.HasOne(standup => standup.Team)
            .WithMany(team => team.Standups)
            .HasForeignKey(standup => standup.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(standup => standup.Owner)
            .WithMany()
            .HasForeignKey(standup => standup.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(standup => standup.Meeting)
            .WithMany()
            .HasForeignKey(standup => standup.MeetingId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class StandupEntryConfiguration : IEntityTypeConfiguration<StandupEntry>
{
    public void Configure(EntityTypeBuilder<StandupEntry> builder)
    {
        builder.ToTable("StandupEntries");
        builder.ConfigureSoftDelete();
        builder.Property(entry => entry.Yesterday).HasColumnType("text");
        builder.Property(entry => entry.Today).HasColumnType("text");
        builder.Property(entry => entry.Blockers).HasColumnType("text");
        builder.Property(entry => entry.Notes).HasColumnType("text");
        builder.Property(entry => entry.Status).AsStringEnum();
        builder.HasIndex(entry => new { entry.StandupId, entry.UserId }).IsUnique();

        builder.HasOne(entry => entry.Standup)
            .WithMany(standup => standup.Entries)
            .HasForeignKey(entry => entry.StandupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.User)
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

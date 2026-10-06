using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Knowledge;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");
        builder.ConfigureSoftDelete();
        builder.Property(note => note.Title).HasMaxLength(300).IsRequired();
        builder.Property(note => note.Body).HasColumnType("text").IsRequired();
        builder.Property(note => note.Category).HasMaxLength(80);
        builder.HasIndex(note => note.UserId);
        builder.HasIndex(note => new { note.UserId, note.IsPinned });
        builder.HasIndex(note => note.ProjectId);
        builder.HasIndex(note => note.TaskId);
        builder.HasIndex(note => note.MeetingId);
        builder.HasIndex(note => note.LearningTopicId);

        builder.HasOne(note => note.User)
            .WithMany()
            .HasForeignKey(note => note.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(note => note.Project)
            .WithMany()
            .HasForeignKey(note => note.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(note => note.Task)
            .WithMany()
            .HasForeignKey(note => note.TaskId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(note => note.Meeting)
            .WithMany()
            .HasForeignKey(note => note.MeetingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(note => note.LearningTopic)
            .WithMany()
            .HasForeignKey(note => note.LearningTopicId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class NoteTagConfiguration : IEntityTypeConfiguration<NoteTag>
{
    public void Configure(EntityTypeBuilder<NoteTag> builder)
    {
        builder.ToTable("NoteTags");
        builder.ConfigureAudit();
        builder.HasIndex(noteTag => new { noteTag.NoteId, noteTag.TagId }).IsUnique();

        builder.HasOne(noteTag => noteTag.Note)
            .WithMany(note => note.NoteTags)
            .HasForeignKey(noteTag => noteTag.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(noteTag => noteTag.Tag)
            .WithMany(tag => tag.NoteTags)
            .HasForeignKey(noteTag => noteTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

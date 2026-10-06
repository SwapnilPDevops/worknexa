using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Learning;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class LearningTopicConfiguration : IEntityTypeConfiguration<LearningTopic>
{
    public void Configure(EntityTypeBuilder<LearningTopic> builder)
    {
        builder.ToTable("LearningTopics");
        builder.ConfigureSoftDelete();
        builder.Property(topic => topic.Title).HasMaxLength(300).IsRequired();
        builder.Property(topic => topic.Description).HasColumnType("text");
        builder.Property(topic => topic.Category).HasMaxLength(120);
        builder.Property(topic => topic.Status).AsStringEnum();
        builder.Property(topic => topic.Priority).AsStringEnum();
        builder.Property(topic => topic.Goal).HasMaxLength(500);
        builder.HasIndex(topic => new { topic.UserId, topic.Status });

        builder.HasOne(topic => topic.User)
            .WithMany(user => user.LearningTopics)
            .HasForeignKey(topic => topic.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(topic => topic.Parent)
            .WithMany(topic => topic.Children)
            .HasForeignKey(topic => topic.ParentTopicId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class LearningResourceConfiguration : IEntityTypeConfiguration<LearningResource>
{
    public void Configure(EntityTypeBuilder<LearningResource> builder)
    {
        builder.ToTable("LearningResources");
        builder.ConfigureSoftDelete();
        builder.Property(resource => resource.Title).HasMaxLength(300).IsRequired();
        builder.Property(resource => resource.Url).HasMaxLength(2048);
        builder.Property(resource => resource.ResourceType).AsStringEnum();

        builder.HasOne(resource => resource.LearningTopic)
            .WithMany(topic => topic.Resources)
            .HasForeignKey(resource => resource.LearningTopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LearningNoteConfiguration : IEntityTypeConfiguration<LearningNote>
{
    public void Configure(EntityTypeBuilder<LearningNote> builder)
    {
        builder.ToTable("LearningNotes");
        builder.ConfigureSoftDelete();
        builder.Property(note => note.Body).HasColumnType("text").IsRequired();

        builder.HasOne(note => note.LearningTopic)
            .WithMany(topic => topic.Notes)
            .HasForeignKey(note => note.LearningTopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Projects;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", table =>
        {
            table.HasCheckConstraint(
                "CK_Projects_DateRange",
                "\"EndDate\" IS NULL OR \"StartDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
        });
        builder.ConfigureSoftDelete();
        builder.Property(project => project.Name).HasMaxLength(200).IsRequired();
        builder.Property(project => project.Code).HasMaxLength(32).IsRequired();
        builder.Property(project => project.Description).HasColumnType("text");
        builder.Property(project => project.Status).AsStringEnum();
        builder.Property(project => project.Priority).AsStringEnum();
        builder.Property(project => project.Technology).HasMaxLength(120);
        builder.Property(project => project.RepositoryUrl).HasMaxLength(500);
        builder.Property(project => project.DocumentationUrl).HasMaxLength(500);
        builder.HasIndex(project => project.Code).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(project => project.Status);
        builder.HasIndex(project => new { project.TeamId, project.Name });

        builder.HasOne(project => project.Owner)
            .WithMany()
            .HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(project => project.Team)
            .WithMany(team => team.Projects)
            .HasForeignKey(project => project.TeamId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.ConfigureAudit();
        builder.Property(member => member.Role).AsStringEnum();
        builder.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique();

        builder.HasOne(member => member.Project)
            .WithMany(project => project.Members)
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(member => member.User)
            .WithMany(user => user.ProjectMemberships)
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

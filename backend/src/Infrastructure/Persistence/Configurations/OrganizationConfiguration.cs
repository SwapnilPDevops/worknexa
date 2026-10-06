using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Organization;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.ConfigureSoftDelete();
        builder.Property(team => team.Name).HasMaxLength(200).IsRequired();
        builder.Property(team => team.Description).HasMaxLength(2000);
        builder.HasIndex(team => team.Name);
    }
}

internal sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");
        builder.ConfigureAudit();
        builder.Property(member => member.Role).AsStringEnum();
        builder.HasIndex(member => new { member.TeamId, member.UserId }).IsUnique();

        builder.HasOne(member => member.Team)
            .WithMany(team => team.Members)
            .HasForeignKey(member => member.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(member => member.User)
            .WithMany(user => user.TeamMemberships)
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class MemberCompetencyConfiguration : IEntityTypeConfiguration<MemberCompetency>
{
    public void Configure(EntityTypeBuilder<MemberCompetency> builder)
    {
        builder.ToTable("MemberCompetencies");
        builder.ConfigureAudit();
        builder.Property(item => item.Kind).AsStringEnum();
        builder.Property(item => item.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.Kind, item.Name }).IsUnique();

        builder.HasOne(item => item.User)
            .WithMany(user => user.Competencies)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

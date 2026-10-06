using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Identity;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.ConfigureSoftDelete();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(512);
        builder.Property(user => user.IsActive).HasDefaultValue(true);
        builder.Property(user => user.Phone).HasMaxLength(40);
        builder.Property(user => user.PhotoUrl).HasMaxLength(500);
        builder.Property(user => user.Designation).HasMaxLength(120);
        builder.Property(user => user.Department).HasMaxLength(120);
        builder.Property(user => user.Status).AsStringEnum();
        builder.Property(user => user.Notes).HasColumnType("text");
        builder.HasIndex(user => user.Email).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(user => user.IsActive);

        builder.HasOne(user => user.Manager)
            .WithMany(user => user.DirectReports)
            .HasForeignKey(user => user.ManagerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.ConfigureSoftDelete();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(500);
        builder.HasIndex(role => role.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.ConfigureSoftDelete();
        builder.Property(permission => permission.Key).HasMaxLength(128).IsRequired();
        builder.Property(permission => permission.Description).HasMaxLength(500);
        builder.HasIndex(permission => permission.Key).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.ConfigureAudit();
        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();

        builder.HasOne(userRole => userRole.User)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(userRole => userRole.Role)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.ConfigureAudit();
        builder.HasIndex(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId }).IsUnique();

        builder.HasOne(rolePermission => rolePermission.Role)
            .WithMany(role => role.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rolePermission => rolePermission.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

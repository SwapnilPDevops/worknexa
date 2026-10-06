using Microsoft.EntityFrameworkCore;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Leaves;

namespace Worknexa.Infrastructure.Persistence.Seed;

internal static class WorknexaSeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid AdminUserId = Guid.Parse("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d");
    public static readonly Guid ManagerUserId = Guid.Parse("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e");
    public static readonly Guid MemberUserId = Guid.Parse("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f");
    public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid TeamMemberRoleId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid ManagerRoleId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    public static readonly Guid AdminUserRoleId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    public static readonly Guid ManagerUserRoleId = Guid.Parse("55555555-5555-4555-8555-555555555555");
    public static readonly Guid MemberUserRoleId = Guid.Parse("66666666-6666-4666-8666-666666666666");
    public static readonly Guid AnnualLeaveTypeId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    public static readonly Guid SickLeaveTypeId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    public static readonly Guid UnpaidLeaveTypeId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3");

    public const string DevelopmentPasswordHash = "AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==";

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            User(AdminUserId, "admin@worknexa.local", "Worknexa Admin"),
            User(ManagerUserId, "manager@worknexa.local", "Worknexa Manager"),
            User(MemberUserId, "member@worknexa.local", "Worknexa Member"));

        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = AdminRoleId,
                Name = RoleNames.Admin,
                Description = "Full access to the workspace.",
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            },
            new Role
            {
                Id = ManagerRoleId,
                Name = RoleNames.Manager,
                Description = "Manages day-to-day work without administering accounts.",
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            },
            new Role
            {
                Id = TeamMemberRoleId,
                Name = RoleNames.TeamMember,
                Description = "Standard access for day-to-day work.",
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            });

        modelBuilder.Entity<Permission>().HasData(PermissionCatalog.All.Select((permission, index) => new Permission
        {
            Id = PermissionId(index + 1),
            Key = permission.Key,
            Description = permission.Description,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        }).ToArray());

        modelBuilder.Entity<RolePermission>().HasData(Grants(AdminRoleId, "d1000001", PermissionCatalog.All.Select(permission => permission.Key).ToArray()));
        modelBuilder.Entity<RolePermission>().HasData(Grants(ManagerRoleId, "d3000001", PermissionCatalog.ManagerKeys));
        modelBuilder.Entity<RolePermission>().HasData(Grants(TeamMemberRoleId, "d2000001", PermissionCatalog.TeamMemberKeys));

        modelBuilder.Entity<UserRole>().HasData(
            Assignment(AdminUserRoleId, AdminUserId, AdminRoleId),
            Assignment(ManagerUserRoleId, ManagerUserId, ManagerRoleId),
            Assignment(MemberUserRoleId, MemberUserId, TeamMemberRoleId));

        modelBuilder.Entity<LeaveType>().HasData(
            new LeaveType
            {
                Id = AnnualLeaveTypeId,
                Name = "Annual",
                IsPaid = true,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            },
            new LeaveType
            {
                Id = SickLeaveTypeId,
                Name = "Sick",
                IsPaid = true,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            },
            new LeaveType
            {
                Id = UnpaidLeaveTypeId,
                Name = "Unpaid",
                IsPaid = false,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            });
    }

    private static User User(Guid id, string email, string displayName)
    {
        return new User
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            PasswordHash = DevelopmentPasswordHash,
            IsActive = true,
            Status = MemberStatus.Active,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        };
    }

    private static UserRole Assignment(Guid id, Guid userId, Guid roleId)
    {
        return new UserRole
        {
            Id = id,
            UserId = userId,
            RoleId = roleId,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        };
    }

    private static RolePermission[] Grants(Guid roleId, string prefix, IReadOnlyList<string> keys)
    {
        return keys.Select((key, index) => new RolePermission
        {
            Id = GrantId(prefix, index + 1),
            RoleId = roleId,
            PermissionId = PermissionId(PermissionNumber(key)),
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        }).ToArray();
    }

    private static int PermissionNumber(string key)
    {
        var index = PermissionCatalog.All.Select((permission, number) => (permission.Key, number)).First(item => item.Key == key).number;
        return index + 1;
    }

    private static Guid PermissionId(int number) => GrantId("c1000001", number);

    private static Guid GrantId(string prefix, int number) =>
        Guid.Parse($"{prefix}-0000-4000-8000-{number:000000000000}");
}

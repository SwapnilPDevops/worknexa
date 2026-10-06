using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Leaves;

namespace Worknexa.Infrastructure.Persistence.Configurations;

internal sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.ConfigureSoftDelete();
        builder.Property(leaveType => leaveType.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(leaveType => leaveType.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class LeaveConfiguration : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> builder)
    {
        builder.ToTable("Leaves", table =>
        {
            table.HasCheckConstraint("CK_Leaves_DateRange", "\"EndDate\" >= \"StartDate\"");
        });
        builder.ConfigureSoftDelete();
        builder.Property(leave => leave.Status).AsStringEnum();
        builder.Property(leave => leave.Reason).HasMaxLength(2000);
        builder.HasIndex(leave => new { leave.UserId, leave.StartDate });
        builder.HasIndex(leave => leave.Status);

        builder.HasOne(leave => leave.User)
            .WithMany(user => user.Leaves)
            .HasForeignKey(leave => leave.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(leave => leave.LeaveType)
            .WithMany(leaveType => leaveType.Leaves)
            .HasForeignKey(leave => leave.LeaveTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(leave => leave.ReviewedBy)
            .WithMany()
            .HasForeignKey(leave => leave.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

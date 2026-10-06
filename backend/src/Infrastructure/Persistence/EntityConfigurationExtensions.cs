using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Worknexa.Domain.Common;
using Worknexa.Domain.Identity;

namespace Worknexa.Infrastructure.Persistence;

internal static class EntityConfigurationExtensions
{
    public static void ConfigureAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }

    public static void ConfigureSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : SoftDeletableEntity
    {
        builder.ConfigureAudit();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.DeletedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entity => entity.DeletedAt);
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
    }

    public static PropertyBuilder<TEnum> AsStringEnum<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum
    {
        return property.HasConversion<string>().HasMaxLength(32).IsRequired();
    }
}

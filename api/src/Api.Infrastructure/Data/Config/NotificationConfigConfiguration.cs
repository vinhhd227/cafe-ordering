using Api.Core.Aggregates.NotificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class NotificationConfigConfiguration : IEntityTypeConfiguration<NotificationConfig>
{
    public void Configure(EntityTypeBuilder<NotificationConfig> builder)
    {
        builder.ToTable("NotificationConfigs");

        builder.Property(c => c.Type).HasMaxLength(50).IsRequired();

        builder.HasIndex(c => c.Type)
            .IsUnique()
            .HasDatabaseName("IX_NotificationConfigs_Type");

        builder.Property(c => c.TargetRoles)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}

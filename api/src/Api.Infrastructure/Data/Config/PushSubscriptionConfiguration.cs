using Api.Core.Aggregates.PushSubscriptionAggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(s => s.P256dh)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.Auth)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(s => s.Endpoint).IsUnique();
        builder.HasIndex(s => s.UserId);
    }
}

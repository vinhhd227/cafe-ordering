using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
  public void Configure(EntityTypeBuilder<Order> builder)
  {
    builder.ToTable("Orders");

    builder.HasKey(o => o.Id);

    builder.Property(o => o.OrderNumber)
      .HasMaxLength(50)
      .IsRequired();

    builder.Property(o => o.SessionId).IsRequired();

    builder.Property(o => o.CustomerId).HasMaxLength(36);

    builder.Property(o => o.DeviceToken).HasMaxLength(200);

    // OrderStatus is a SmartEnum (class) — map via value converter using its int Value
    builder.Property(o => o.Status)
      .IsRequired()
      .HasConversion(
        v => v.Value,
        v => OrderStatus.FromValue(v));

    builder.Property(o => o.OrderDate).IsRequired();

    // FK: Order → GuestSession
    builder.HasOne<GuestSession>()
      .WithMany()
      .HasForeignKey(o => o.SessionId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(o => o.SessionId)
      .HasDatabaseName("IX_Orders_SessionId");

    builder.HasIndex(o => o.CustomerId)
      .HasDatabaseName("IX_Orders_CustomerId");

    // Concurrency token
    builder.Property(o => o.RowVersion)
      .IsRowVersion();
  }
}

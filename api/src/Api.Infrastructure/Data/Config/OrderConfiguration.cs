using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.OrderAggregate;

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

    builder.Property(o => o.SessionId);  // nullable — Takeaway/Delivery orders have no session

    builder.Property(o => o.CustomerId).HasMaxLength(36);

    builder.Property(o => o.DeviceToken).HasMaxLength(200);

    builder.Property(o => o.Type)
      .IsRequired()
      .HasMaxLength(20)
      .HasDefaultValueSql("'DINE_IN'")
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => OrderType.FromName(v, true));

    builder.Property(o => o.CustomerName).HasMaxLength(150);
    builder.Property(o => o.CustomerPhone).HasMaxLength(20);
    builder.Property(o => o.DeliveryAddress).HasMaxLength(500);
    builder.Property(o => o.DeliveryNote).HasMaxLength(500);

    // OrderStatus is a SmartEnum — store as UPPERCASE string for readability and correct EF Core translation
    builder.Property(o => o.Status)
      .IsRequired()
      .HasMaxLength(20)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => OrderStatus.FromName(v, true));

    builder.Property(o => o.PaymentStatus)
      .IsRequired()
      .HasMaxLength(20)
      .HasDefaultValueSql("'UNPAID'")
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PaymentStatus.FromName(v, true));

    builder.Property(o => o.PaymentMethod)
      .IsRequired()
      .HasMaxLength(20)
      .HasDefaultValueSql("'UNKNOWN'")
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PaymentMethod.FromName(v, true));

    builder.Property(o => o.GuestCount);
    builder.Property(o => o.AmountReceived);
    builder.Property(o => o.TipAmount).IsRequired().HasDefaultValue(0m);

    builder.Property(o => o.OrderDate).IsRequired();
    builder.Property(o => o.CompletedAt);
    builder.Property(o => o.PaidAt);

    // FK: Order → GuestSession (optional — null for Takeaway/Delivery)
    builder.HasOne<GuestSession>()
      .WithMany()
      .HasForeignKey(o => o.SessionId)
      .IsRequired(false)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(o => o.SessionId)
      .HasDatabaseName("IX_Orders_SessionId");

    builder.HasIndex(o => o.Type)
      .HasDatabaseName("IX_Orders_Type");

    builder.HasIndex(o => o.CustomerId)
      .HasDatabaseName("IX_Orders_CustomerId");

    // OrderItems — explicit config (EF auto-discovers _items backing field via convention)
    builder.HasMany(o => o.Items)
      .WithOne()
      .HasForeignKey(i => i.OrderId)
      .OnDelete(DeleteBehavior.Cascade);

    // OrderPromotions — use public property (EF auto-discovers _promotions backing field via convention)
    builder.HasMany(o => o.Promotions)
      .WithOne()
      .HasForeignKey(op => op.OrderId)
      .OnDelete(DeleteBehavior.Cascade);

    // Concurrency token
    builder.Property(o => o.RowVersion)
      .IsRowVersion();

    builder.HasIndex(o => o.OrderDate)
      .HasDatabaseName("IX_Orders_OrderDate");

    builder.HasIndex(o => o.Status)
      .HasDatabaseName("IX_Orders_Status");

    builder.HasIndex(o => o.OrderNumber)
      .IsUnique()
      .HasDatabaseName("IX_Orders_OrderNumber_Unique");
  }
}

using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.OrderAggregate;

namespace Api.Infrastructure.Data.Config;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
  public void Configure(EntityTypeBuilder<Expense> builder)
  {
    builder.ToTable("Expenses");

    builder.HasKey(e => e.Id);

    builder.Property(e => e.Name)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(e => e.Unit)
      .HasMaxLength(50)
      .IsRequired(false);

    builder.Property(e => e.Notes)
      .HasMaxLength(500)
      .IsRequired(false);

    builder.Property(e => e.Quantity)
      .HasPrecision(18, 3)
      .IsRequired();

    builder.Property(e => e.UnitPrice)
      .HasPrecision(18, 2)
      .IsRequired();

    builder.Property(e => e.TotalAmount)
      .HasPrecision(18, 2)
      .IsRequired();

    builder.Property(e => e.Category)
      .HasConversion(
        v => v.Name,
        v => ExpenseCategory.FromName(v, true))
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(e => e.PaymentMethod)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PaymentMethod.FromName(v, true))
      .HasMaxLength(20)
      .HasDefaultValueSql("'CASH'")
      .IsRequired();

    builder.HasIndex(e => e.Category);
    builder.HasIndex(e => e.PurchaseDate);
    builder.HasIndex(e => e.IsDeleted);

    // Concurrency token
    builder.Property(e => e.RowVersion)
      .IsRowVersion();
  }
}

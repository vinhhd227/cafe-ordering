using Api.Core.Aggregates.PromotionAggregate;

namespace Api.Infrastructure.Data.Config;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
  public void Configure(EntityTypeBuilder<Promotion> builder)
  {
    builder.ToTable("Promotions");

    builder.HasKey(p => p.Id);

    builder.Property(p => p.Name)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(p => p.Code)
      .HasMaxLength(50)
      .IsRequired(false);

    builder.Property(p => p.Description)
      .HasMaxLength(500)
      .IsRequired(false);

    builder.Property(p => p.DiscountType)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => DiscountType.FromName(v, true))
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(p => p.DiscountValue)
      .HasPrecision(18, 2)
      .IsRequired();

    builder.Property(p => p.Scope)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PromotionScope.FromName(v, true))
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(p => p.StackPolicy)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => StackPolicy.FromName(v, true))
      .HasMaxLength(20)
      .HasDefaultValueSql("'EXCLUSIVE'")
      .IsRequired();

    // Store list<int> as jsonb in PostgreSQL
    builder.Property(p => p.ApplicableProductIds)
      .HasColumnType("jsonb")
      .IsRequired();

    builder.Property(p => p.ApplicableCategoryIds)
      .HasColumnType("jsonb")
      .IsRequired();

    builder.Property(p => p.MinOrderAmount)
      .HasPrecision(18, 2)
      .IsRequired(false);

    builder.Property(p => p.StartDate).IsRequired();
    builder.Property(p => p.EndDate).IsRequired(false);
    builder.Property(p => p.MaxUsage).IsRequired(false);
    builder.Property(p => p.CurrentUsage).IsRequired().HasDefaultValue(0);
    builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

    // Code is unique when not null (partial unique index)
    builder.HasIndex(p => p.Code)
      .IsUnique()
      .HasFilter("\"Code\" IS NOT NULL")
      .HasDatabaseName("IX_Promotions_Code_Unique");

    builder.HasIndex(p => p.IsActive)
      .HasDatabaseName("IX_Promotions_IsActive");

    builder.HasIndex(p => new { p.StartDate, p.EndDate })
      .HasDatabaseName("IX_Promotions_DateRange");

    builder.HasIndex(p => p.IsDeleted)
      .HasDatabaseName("IX_Promotions_IsDeleted");

    // Concurrency token
    builder.Property(p => p.RowVersion)
      .IsRowVersion();
  }
}

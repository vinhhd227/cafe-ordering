using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
  public void Configure(EntityTypeBuilder<ProductVariant> builder)
  {
    builder.ToTable("ProductVariants");

    builder.HasKey(v => v.Id);

    builder.Property(v => v.Price)
      .HasPrecision(18, 2)
      .IsRequired();

    builder.Property(v => v.CostPrice)
      .HasPrecision(18, 2);

    builder.Property(v => v.Sku)
      .HasMaxLength(100);

    builder.Property(v => v.Barcode)
      .HasMaxLength(100);

    builder.HasMany(v => v.Values)
      .WithOne(v => v.ProductVariant)
      .HasForeignKey(v => v.ProductVariantId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(v => v.ProductId);
    builder.HasIndex(v => new { v.ProductId, v.DisplayOrder });
    builder.HasIndex(v => v.IsActive);
  }
}

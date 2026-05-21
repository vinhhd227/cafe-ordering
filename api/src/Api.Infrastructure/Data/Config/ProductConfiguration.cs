using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
  public void Configure(EntityTypeBuilder<Product> builder)
  {
    builder.ToTable("Products");

    builder.HasKey(p => p.Id);

    builder.Property(p => p.Name)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(p => p.Description)
      .HasMaxLength(2000);

    builder.Property(p => p.Price)
      .HasPrecision(18, 2);

    builder.Property(p => p.CostPrice)
      .HasPrecision(18, 2);

    builder.Property(p => p.DiscountPrice)
      .HasPrecision(18, 2);

    builder.Property(p => p.Sku)
      .HasMaxLength(100);

    builder.Property(p => p.Barcode)
      .HasMaxLength(100);

    builder.Property(p => p.ImageUrl)
      .HasMaxLength(500);

    // Relationship: Product â†’ Category (nullable â€” product can exist without category)
    builder.HasOne(p => p.Category)
      .WithMany()
      .HasForeignKey(p => p.CategoryId)
      .OnDelete(DeleteBehavior.Restrict)
      .IsRequired(false);

    builder.Property(p => p.IsAccompaniment)
      .IsRequired()
      .HasDefaultValue(false);

    builder.Property(p => p.EstimatedPrepMinutes);

    builder.HasMany(p => p.VariantGroups)
      .WithOne()
      .HasForeignKey(g => g.ProductId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(p => p.Variants)
      .WithOne()
      .HasForeignKey(v => v.ProductId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(p => p.OptionGroupMappings)
      .WithOne()
      .HasForeignKey(m => m.ProductId)
      .OnDelete(DeleteBehavior.Cascade);

    // Indexes
    builder.HasIndex(p => p.CategoryId);
    builder.HasIndex(p => p.IsActive);
    builder.HasIndex(p => p.Name);

    // Concurrency token
    builder.Property(p => p.RowVersion)
      .IsRowVersion();
  }
}

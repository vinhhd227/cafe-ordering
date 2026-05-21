using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductVariantSelectionConfiguration : IEntityTypeConfiguration<ProductVariantSelection>
{
  public void Configure(EntityTypeBuilder<ProductVariantSelection> builder)
  {
    builder.ToTable("ProductVariantSelections");

    builder.HasKey(v => v.Id);

    builder.HasOne(v => v.Value)
      .WithMany()
      .HasForeignKey(v => v.ProductVariantValueId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(v => v.ProductVariantId);
    builder.HasIndex(v => new { v.ProductVariantId, v.ProductVariantValueId })
      .IsUnique();
  }
}

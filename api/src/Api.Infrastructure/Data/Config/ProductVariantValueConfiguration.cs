using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductVariantValueConfiguration : IEntityTypeConfiguration<ProductVariantValue>
{
  public void Configure(EntityTypeBuilder<ProductVariantValue> builder)
  {
    builder.ToTable("ProductVariantValues");

    builder.HasKey(v => v.Id);

    builder.Property(v => v.Label)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(v => v.Price)
      .HasPrecision(18, 2);

    builder.HasIndex(v => v.GroupId);
  }
}

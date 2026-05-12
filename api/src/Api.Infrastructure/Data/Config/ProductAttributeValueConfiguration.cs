using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
  public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
  {
    builder.ToTable("ProductAttributeValues");

    builder.HasKey(v => v.Id);

    builder.Property(v => v.Label)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(v => v.PriceAdjustment)
      .HasPrecision(18, 2);

    builder.HasIndex(v => v.GroupId);
  }
}

using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductVariantGroupConfiguration : IEntityTypeConfiguration<ProductVariantGroup>
{
  public void Configure(EntityTypeBuilder<ProductVariantGroup> builder)
  {
    builder.ToTable("ProductVariantGroups");

    builder.HasKey(g => g.Id);

    builder.Property(g => g.Name)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(g => g.SelectionType)
      .HasConversion<int>();

    builder.HasMany(g => g.Values)
      .WithOne()
      .HasForeignKey(v => v.GroupId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(g => g.ProductId);
    builder.HasIndex(g => new { g.ProductId, g.DisplayOrder });
  }
}

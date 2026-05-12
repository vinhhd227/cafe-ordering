using Api.Core.Aggregates.ProductOptionGroupAggregate;

namespace Api.Infrastructure.Data.Config;

public class ProductOptionGroupConfiguration : IEntityTypeConfiguration<ProductOptionGroup>
{
  public void Configure(EntityTypeBuilder<ProductOptionGroup> builder)
  {
    builder.ToTable("ProductOptionGroups");

    builder.HasKey(g => g.Id);

    builder.Property(g => g.Name)
      .HasMaxLength(100)
      .IsRequired();

    builder.HasMany(g => g.Values)
      .WithOne()
      .HasForeignKey(v => v.GroupId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(g => g.IsActive);
    builder.HasIndex(g => g.DisplayOrder);
  }
}

public class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
  public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
  {
    builder.ToTable("ProductOptionValues");

    builder.HasKey(v => v.Id);

    builder.Property(v => v.Name)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(v => v.Price)
      .HasPrecision(18, 2);

    builder.Property(v => v.CostPrice)
      .HasPrecision(18, 2);

    builder.HasIndex(v => v.GroupId);
    builder.HasIndex(v => new { v.GroupId, v.DisplayOrder });
  }
}

public class ProductOptionGroupMappingConfiguration : IEntityTypeConfiguration<ProductOptionGroupMapping>
{
  public void Configure(EntityTypeBuilder<ProductOptionGroupMapping> builder)
  {
    builder.ToTable("ProductOptionGroupMappings");

    builder.HasKey(m => m.Id);

    builder.HasOne(m => m.Group)
      .WithMany(g => g.Mappings)
      .HasForeignKey(m => m.GroupId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(m => m.ProductId);
    builder.HasIndex(m => m.GroupId);
    builder.HasIndex(m => new { m.ProductId, m.GroupId }).IsUnique();
  }
}

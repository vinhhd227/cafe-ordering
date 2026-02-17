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

    builder.Property(p => p.ImageUrl)
      .HasMaxLength(500);

    // Relationship: Product → Category
    builder.HasOne(p => p.Category)
      .WithMany()
      .HasForeignKey(p => p.CategoryId)
      .OnDelete(DeleteBehavior.Restrict);

    // Indexes
    builder.HasIndex(p => p.CategoryId);
    builder.HasIndex(p => p.IsActive);
    builder.HasIndex(p => p.Name);

    // Concurrency token
    builder.Property(p => p.RowVersion)
      .IsRowVersion();
  }
}

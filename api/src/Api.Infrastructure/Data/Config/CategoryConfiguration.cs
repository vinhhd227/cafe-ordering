using Api.Core.Aggregates.CategoryAggregate;

namespace Api.Infrastructure.Data.Config;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
  public void Configure(EntityTypeBuilder<Category> builder)
  {
    builder.ToTable("Categories");

    builder.HasKey(c => c.Id);

    builder.Property(c => c.Name)
      .HasMaxLength(100)
      .IsRequired();

    // Unique name chỉ cho records chưa bị xóa (PostgreSQL syntax)
    builder.HasIndex(c => c.Name)
      .IsUnique()
      .HasFilter(@"""IsDeleted"" = false");

    builder.HasIndex(c => c.IsActive);

    // Concurrency token
    builder.Property(c => c.RowVersion)
      .IsRowVersion();
  }
}

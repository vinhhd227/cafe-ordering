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

    builder.Property(c => c.Description)
      .HasMaxLength(500)
      .IsRequired(false);

    builder.Property(c => c.ImageUrl)
      .HasMaxLength(2048)
      .IsRequired(false);

    builder.Property(c => c.SortOrder)
      .IsRequired()
      .HasDefaultValue(0);

    // Unique name chỉ cho records chưa bị xóa (PostgreSQL syntax)
    builder.HasIndex(c => c.Name)
      .IsUnique()
      .HasFilter(@"""IsDeleted"" = false");

    builder.HasIndex(c => c.SortOrder);
    builder.HasIndex(c => c.IsActive);

    // Concurrency token
    builder.Property(c => c.RowVersion)
      .IsRowVersion();
  }
}

using Api.Core.Aggregates.RecipeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
  public void Configure(EntityTypeBuilder<Recipe> builder)
  {
    builder.ToTable("Recipes");

    builder.HasKey(r => r.Id);

    builder.Property(r => r.Name)
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(r => r.Type)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(30);

    builder.Property(r => r.Category)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(30);

    builder.Property(r => r.Content)
      .IsRequired()
      .HasMaxLength(10000);

    builder.Property(r => r.Yield)
      .IsRequired(false)
      .HasMaxLength(200);

    builder.Property(r => r.Notes)
      .IsRequired(false)
      .HasMaxLength(2000);

    builder.HasIndex(r => r.Name)
      .HasFilter(@"""IsDeleted"" = false")
      .HasDatabaseName("IX_Recipes_Name");

    builder.HasIndex(r => r.Category)
      .HasDatabaseName("IX_Recipes_Category");

    builder.Property(r => r.RowVersion).IsRowVersion();
  }
}

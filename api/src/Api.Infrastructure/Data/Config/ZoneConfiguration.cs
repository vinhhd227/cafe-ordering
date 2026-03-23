using Api.Core.Aggregates.ZoneAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
  public void Configure(EntityTypeBuilder<Zone> builder)
  {
    builder.ToTable("Zones");

    builder.HasKey(z => z.Id);

    builder.Property(z => z.Name)
      .IsRequired()
      .HasMaxLength(50);

    builder.Property(z => z.DisplayOrder)
      .HasDefaultValue(0);

    builder.HasIndex(z => z.Name)
      .IsUnique()
      .HasFilter(@"""IsDeleted"" = false")
      .HasDatabaseName("IX_Zones_Name");

    builder.Property(z => z.RowVersion)
      .IsRowVersion();
  }
}

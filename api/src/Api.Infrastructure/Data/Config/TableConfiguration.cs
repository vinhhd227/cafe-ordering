using Api.Core.Aggregates.TableAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
  public void Configure(EntityTypeBuilder<Table> builder)
  {
    builder.ToTable("Tables");

    builder.HasKey(t => t.Id);

    builder.Property(t => t.Number).IsRequired();

    builder.Property(t => t.Code).IsRequired().HasMaxLength(20);

    builder.Property(t => t.Status)
      .IsRequired()
      .HasDefaultValue(TableStatus.Available);

    builder.Property(t => t.ActiveSessionId);

    // Unique table number among non-deleted tables
    builder.HasIndex(t => t.Number)
      .IsUnique()
      .HasFilter(@"""IsDeleted"" = false")
      .HasDatabaseName("IX_Tables_Number");

    // Unique table code among non-deleted tables
    builder.HasIndex(t => t.Code)
      .IsUnique()
      .HasFilter(@"""IsDeleted"" = false")
      .HasDatabaseName("IX_Tables_Code");

    builder.HasIndex(t => t.Status)
      .HasDatabaseName("IX_Tables_Status");

    // Concurrency token
    builder.Property(t => t.RowVersion)
      .IsRowVersion();
  }
}

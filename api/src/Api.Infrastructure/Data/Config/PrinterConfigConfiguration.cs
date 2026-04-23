using Api.Core.Aggregates.PrinterAggregate;

namespace Api.Infrastructure.Data.Config;

public class PrinterConfigConfiguration : IEntityTypeConfiguration<PrinterConfig>
{
  public void Configure(EntityTypeBuilder<PrinterConfig> builder)
  {
    builder.ToTable("PrinterConfigs");

    builder.Property(p => p.Name)
      .IsRequired()
      .HasMaxLength(100);

    builder.Property(p => p.Role)
      .IsRequired()
      .HasMaxLength(20)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PrinterRole.FromName(v, true));

    builder.Property(p => p.FormatterType)
      .IsRequired()
      .HasMaxLength(20)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PrintFormatterType.FromName(v, true));

    builder.Property(p => p.TransportType)
      .IsRequired()
      .HasMaxLength(20)
      .HasConversion(
        v => v.Name.ToUpperInvariant(),
        v => PrintTransportType.FromName(v, true));

    builder.Property(p => p.ConnectionParams)
      .IsRequired()
      .HasMaxLength(500);

    builder.Property(p => p.PaperWidthMm).IsRequired();
    builder.Property(p => p.IsDefault).IsRequired().HasDefaultValue(false);
    builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

    builder.HasIndex(p => new { p.Role, p.IsDefault })
      .HasDatabaseName("IX_PrinterConfigs_Role_IsDefault");
  }
}

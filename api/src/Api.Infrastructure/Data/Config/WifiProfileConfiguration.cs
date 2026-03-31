using Api.Core.Aggregates.WifiProfileAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class WifiProfileConfiguration : IEntityTypeConfiguration<WifiProfile>
{
  public void Configure(EntityTypeBuilder<WifiProfile> builder)
  {
    builder.ToTable("WifiProfiles");

    builder.HasKey(w => w.Id);

    builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
    builder.Property(w => w.Ssid).IsRequired().HasMaxLength(100);
    builder.Property(w => w.Password).IsRequired(false).HasMaxLength(200);
    builder.Property(w => w.SecurityType).IsRequired().HasMaxLength(10);
    builder.Property(w => w.ForegroundColor).IsRequired().HasMaxLength(7);
    builder.Property(w => w.BackgroundColor).IsRequired().HasMaxLength(7);
    builder.Property(w => w.CustomText).IsRequired(false).HasMaxLength(200);

    builder.Property(w => w.RowVersion).IsRowVersion();
  }
}

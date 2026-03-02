using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.TableAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class GuestSessionConfiguration : IEntityTypeConfiguration<GuestSession>
{
  public void Configure(EntityTypeBuilder<GuestSession> builder)
  {
    builder.ToTable("GuestSessions");

    builder.HasKey(s => s.Id);

    builder.Property(s => s.TableId).IsRequired(false);

    builder.Property(s => s.Status)
      .IsRequired()
      .HasDefaultValue(GuestSessionStatus.Active);

    builder.Property(s => s.OpenedAt).IsRequired();

    builder.Property(s => s.ClosedAt);

    builder.Property(s => s.CustomerId).HasMaxLength(36);

    // FK: GuestSession → Table (optional for counter sessions)
    builder.HasOne<Table>()
      .WithMany()
      .HasForeignKey(s => s.TableId)
      .IsRequired(false)
      .OnDelete(DeleteBehavior.Restrict);

    // Index for active session lookups
    builder.HasIndex(s => new { s.TableId, s.Status })
      .HasDatabaseName("IX_GuestSessions_TableId_Status");
    
    builder.HasIndex(s => s.TableId)
      .HasDatabaseName("IX_GuestSessions_ActiveByTable")
      .HasFilter("\"Status\" = 1");
  }
  
}

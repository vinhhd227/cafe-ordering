using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Identity.Config;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
  public void Configure(EntityTypeBuilder<ApplicationRole> builder)
  {
    builder.ToTable("Roles");

    builder.HasKey(r => r.Id);

    builder.HasIndex(r => r.NormalizedName)
      .IsUnique()
      .HasDatabaseName("RoleNameIndex");

    builder.Property(r => r.Name).HasMaxLength(256).IsRequired();
    builder.Property(r => r.NormalizedName).HasMaxLength(256).IsRequired();
    builder.Property(r => r.Description).HasMaxLength(500);
    builder.Property(r => r.ConcurrencyStamp).HasMaxLength(100);
    builder.Property(r => r.CreatedAt).IsRequired();

    builder.HasMany(r => r.UserRoles)
      .WithOne(ur => ur.Role)
      .HasForeignKey(ur => ur.RoleId)
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);
  }
}

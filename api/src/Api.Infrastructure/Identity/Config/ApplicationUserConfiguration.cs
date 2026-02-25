using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Identity.Config;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
  public void Configure(EntityTypeBuilder<ApplicationUser> builder)
  {
    builder.ToTable("Users");

    builder.HasKey(u => u.Id);

    builder.HasIndex(u => u.NormalizedUserName)
      .IsUnique()
      .HasDatabaseName("UserNameIndex");

    builder.HasIndex(u => u.NormalizedEmail)
      .HasDatabaseName("EmailIndex");

    builder.HasIndex(u => u.IsActive);

    builder.Property(u => u.UserName).HasMaxLength(256).IsRequired();
    builder.Property(u => u.NormalizedUserName).HasMaxLength(256).IsRequired();
    builder.Property(u => u.Email).HasMaxLength(256);
    builder.Property(u => u.NormalizedEmail).HasMaxLength(256);
    builder.Property(u => u.PhoneNumber).HasMaxLength(20);
    builder.Property(u => u.PasswordHash).HasMaxLength(500);
    builder.Property(u => u.SecurityStamp).HasMaxLength(100);
    builder.Property(u => u.ConcurrencyStamp).HasMaxLength(100);

    // New fields
    builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
    builder.Property(u => u.StaffId);
    builder.Property(u => u.CustomerId);

    builder.HasMany(u => u.UserRoles)
      .WithOne(ur => ur.User)
      .HasForeignKey(ur => ur.UserId)
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);

    // RefreshTokens navigation — actual FK config lives in RefreshTokenConfiguration
    builder.HasMany(u => u.RefreshTokens)
      .WithOne(t => t.User)
      .HasForeignKey(t => t.UserId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Identity.Config;

public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
  public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
  {
    builder.ToTable("UserRefreshTokens");

    builder.HasKey(t => t.Id);

    builder.Property(t => t.Token)
      .HasMaxLength(256)
      .IsRequired();

    builder.Property(t => t.DeviceInfo)
      .HasMaxLength(512);

    builder.Property(t => t.RevokedReason)
      .HasMaxLength(256);

    // Look up by token value (used on every refresh)
    builder.HasIndex(t => t.Token)
      .IsUnique()
      .HasDatabaseName("IX_UserRefreshTokens_Token");

    // Look up all tokens by user (used for logout-all-devices)
    builder.HasIndex(t => t.UserId)
      .HasDatabaseName("IX_UserRefreshTokens_UserId");

    // Composite index: active tokens per user
    builder.HasIndex(t => new { t.UserId, t.RevokedAt, t.ExpiresAt })
      .HasDatabaseName("IX_UserRefreshTokens_UserId_Active");

    builder.HasOne(t => t.User)
      .WithMany(u => u.RefreshTokens)
      .HasForeignKey(t => t.UserId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}

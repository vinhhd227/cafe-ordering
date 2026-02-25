using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Identity.Config;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
  public void Configure(EntityTypeBuilder<RefreshToken> builder)
  {
    builder.ToTable("RefreshTokens");

    builder.HasKey(t => t.Id);

    builder.Property(t => t.Token)
      .HasMaxLength(256)
      .IsRequired();

    // Look up by token value (used on every refresh)
    builder.HasIndex(t => t.Token)
      .IsUnique()
      .HasDatabaseName("IX_RefreshTokens_Token");

    // Look up all tokens by user (used for logout-all-devices)
    builder.HasIndex(t => t.UserId)
      .HasDatabaseName("IX_RefreshTokens_UserId");

    builder.HasOne(t => t.User)
      .WithMany(u => u.RefreshTokens)
      .HasForeignKey(t => t.UserId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}

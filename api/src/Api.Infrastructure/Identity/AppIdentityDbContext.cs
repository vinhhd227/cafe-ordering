using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Identity-only DbContext. Handles authentication data:
/// Users, Roles, Claims, Tokens — separated from business data.
/// </summary>
public class AppIdentityDbContext : IdentityDbContext<
  ApplicationUser,
  ApplicationRole,
  int,
  IdentityUserClaim<int>,
  ApplicationUserRole,
  IdentityUserLogin<int>,
  IdentityRoleClaim<int>,
  IdentityUserToken<int>>
{
  public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
  {
  }

  /// <summary>
  /// One row per active session. Supports multi-device login.
  /// </summary>
  public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    // Schema phải đặt TRƯỚC base.OnModelCreating() để Identity
    // áp dụng schema cho tất cả tables nó tạo ra (AspNetUsers, etc.)
    builder.HasDefaultSchema("identity");

    base.OnModelCreating(builder);

    builder.ApplyConfigurationsFromAssembly(
      typeof(AppIdentityDbContext).Assembly,
      t => t.Namespace?.StartsWith("Api.Infrastructure.Identity.Config") == true);
  }
}

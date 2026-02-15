using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Data.Config.Identity;

public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<int>>
{
  public void Configure(EntityTypeBuilder<IdentityUserClaim<int>> builder)
  {
    builder.ToTable("UserClaims");
    builder.HasKey(uc => uc.Id);
  }
}

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<int>>
{
  public void Configure(EntityTypeBuilder<IdentityUserLogin<int>> builder)
  {
    builder.ToTable("UserLogins");
    builder.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
  }
}

public class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<int>>
{
  public void Configure(EntityTypeBuilder<IdentityRoleClaim<int>> builder)
  {
    builder.ToTable("RoleClaims");
    builder.HasKey(rc => rc.Id);
  }
}

public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<int>>
{
  public void Configure(EntityTypeBuilder<IdentityUserToken<int>> builder)
  {
    builder.ToTable("UserTokens");
    builder.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });
  }
}

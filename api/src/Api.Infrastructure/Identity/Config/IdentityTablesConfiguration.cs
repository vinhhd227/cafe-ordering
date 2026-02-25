using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Identity.Config;

public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
  public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
  {
    builder.ToTable("UserClaims");
    builder.HasKey(uc => uc.Id);
  }
}

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
  public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
  {
    builder.ToTable("UserLogins");
    builder.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
  }
}

public class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
  public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
  {
    builder.ToTable("RoleClaims");
    builder.HasKey(rc => rc.Id);
  }
}

public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
  public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
  {
    builder.ToTable("UserTokens");
    builder.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });
  }
}

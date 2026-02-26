using System.Security.Claims;
using System.Text;
using Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Web.Configurations;

public static class AuthConfiguration
{
  public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
  {
    // JWT Authentication — uses same config keys as JwtService (Infrastructure):
    // Jwt:Key, Jwt:Issuer, Jwt:Audience
    services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = configuration["Jwt:Issuer"],
          ValidAudience = configuration["Jwt:Audience"] ?? configuration["Jwt:Issuer"],
          IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
          ClockSkew = TimeSpan.Zero,
          NameClaimType = ClaimTypes.NameIdentifier,
          RoleClaimType = ClaimTypes.Role
        };
      });

    // Authorization Policies
    services.AddAuthorization(options =>
    {
      options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
      options.AddPolicy("StaffOrAdmin", policy =>
        policy.RequireRole("Staff", "Admin"));

      // Claim-based policies: Admin role bypasses all, others need exact claim
      foreach (var permissionKey in PermissionRegistry.All.Keys)
      {
        var key = permissionKey;
        options.AddPolicy(key, policy =>
          policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("Admin") ||
            ctx.User.HasClaim("permission", key)));
      }
    });

    return services;
  }
}

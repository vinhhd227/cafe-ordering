using System.Security.Claims;
using System.Text;
using Api.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Web.Configurations;

public static class AuthConfiguration
{
  public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
  {
    // JWT Authentication
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
          ValidAudience = configuration["Jwt:Audience"],
          IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
          ClockSkew = TimeSpan.Zero,
          // Map Identity claims to standard claims
          NameClaimType = ClaimTypes.NameIdentifier,
          RoleClaimType = ClaimTypes.Role
        };
      });

    // Authorization Policies
    services.AddAuthorization(options =>
    {
      options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
      options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));
      options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("Barista", "Manager", "Admin"));
    });

    // Token Service for JWT generation
    services.AddScoped<ITokenService, TokenService>();

    return services;
  }
}

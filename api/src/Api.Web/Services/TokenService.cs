using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Api.Web.Services;

public class TokenService : ITokenService
{
  private readonly IConfiguration _configuration;
  private readonly UserManager<ApplicationUser> _userManager;

  public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
  {
    _configuration = configuration;
    _userManager = userManager;
  }

  public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
  {
    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new(ClaimTypes.Name, user.UserName),
      new(ClaimTypes.Email, user.Email),
      new("firstName", user.FirstName),
      new("lastName", user.LastName),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    // Add roles to claims
    var roles = await _userManager.GetRolesAsync(user);
    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var key = new SymmetricSecurityKey(
      Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddMinutes(
      int.Parse(_configuration["Jwt:ExpiresMinutes"] ?? "60"));

    var token = new JwtSecurityToken(
      _configuration["Jwt:Issuer"],
      _configuration["Jwt:Audience"],
      claims,
      expires: expires,
      signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public string GenerateRefreshToken()
  {
    var randomNumber = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    return Convert.ToBase64String(randomNumber);
  }

  public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
  {
    var tokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = false, // Don't validate lifetime for refresh
      ValidateIssuerSigningKey = true,
      ValidIssuer = _configuration["Jwt:Issuer"],
      ValidAudience = _configuration["Jwt:Audience"],
      IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!))
    };

    var tokenHandler = new JwtSecurityTokenHandler();

    try
    {
      var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

      if (securityToken is not JwtSecurityToken jwtSecurityToken ||
          !jwtSecurityToken.Header.Alg.Equals(
            SecurityAlgorithms.HmacSha256,
            StringComparison.InvariantCultureIgnoreCase))
      {
        return null;
      }

      return principal;
    }
    catch
    {
      return null;
    }
  }
}

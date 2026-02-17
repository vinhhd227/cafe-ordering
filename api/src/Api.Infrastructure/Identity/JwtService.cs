using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.UseCases.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Implementation of IJwtService for JWT token generation and validation.
/// </summary>
public class JwtService : IJwtService
{
  private readonly string _secret;
  private readonly string _issuer;
  private readonly int _expiryMinutes;

  public JwtService(IConfiguration configuration)
  {
    _secret = configuration["Jwt:Secret"]
      ?? throw new InvalidOperationException("JWT Secret not configured");
    _issuer = configuration["Jwt:Issuer"]
      ?? throw new InvalidOperationException("JWT Issuer not configured");
    _expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "60");
  }

  public string GenerateAccessToken(int userId, string? customerId, string email, IList<string> roles)
  {
    var claims = new List<Claim>
    {
      new("userId", userId.ToString()),
      new(ClaimTypes.Email, email)
    };

    // customerId only for Customer role users (string type)
    if (!string.IsNullOrEmpty(customerId))
      claims.Add(new Claim("customerId", customerId));

    // Add roles
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: _issuer,
      audience: _issuer,
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public string GenerateRefreshToken()
  {
    var randomBytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);
  }

  public ClaimsPrincipal? ValidateToken(string token)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(_secret);

    try
    {
      var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = _issuer,
        ValidateAudience = true,
        ValidAudience = _issuer,
        ValidateLifetime = false // Don't validate expiry for refresh
      }, out _);

      return principal;
    }
    catch
    {
      return null;
    }
  }
}

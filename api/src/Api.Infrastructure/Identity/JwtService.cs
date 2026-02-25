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
/// Config keys: Jwt:Key, Jwt:Issuer, Jwt:Audience
/// Access token: 15 minutes (fixed)
/// </summary>
public class JwtService : IJwtService
{
  private const int AccessTokenExpiryMinutes = 15;

  private readonly string _key;
  private readonly string _issuer;
  private readonly string _audience;

  public JwtService(IConfiguration configuration)
  {
    _key = configuration["Jwt:Key"]
      ?? throw new InvalidOperationException("JWT Key not configured (Jwt:Key)");
    _issuer = configuration["Jwt:Issuer"]
      ?? throw new InvalidOperationException("JWT Issuer not configured (Jwt:Issuer)");
    _audience = configuration["Jwt:Audience"] ?? _issuer;
  }

  public string GenerateAccessToken(
    Guid userId,
    string username,
    string fullName,
    IList<string> roles,
    IList<string> permissions,
    Guid? staffId = null,
    Guid? customerId = null)
  {
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, userId.ToString()),
      new(ClaimTypes.NameIdentifier, userId.ToString()),
      new("username", username),
      new("fullName", fullName)
    };

    foreach (var role in roles)
      claims.Add(new Claim(ClaimTypes.Role, role));

    foreach (var permission in permissions)
      claims.Add(new Claim("permission", permission));

    if (staffId.HasValue)
      claims.Add(new Claim("staffId", staffId.Value.ToString()));

    if (customerId.HasValue)
      claims.Add(new Claim("customerId", customerId.Value.ToString()));

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: _issuer,
      audience: _audience,
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
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
    var signingKey = Encoding.UTF8.GetBytes(_key);

    try
    {
      var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(signingKey),
        ValidateIssuer = true,
        ValidIssuer = _issuer,
        ValidateAudience = true,
        ValidAudience = _audience,
        ValidateLifetime = false // Don't validate expiry — used for refresh flow
      }, out _);

      return principal;
    }
    catch
    {
      return null;
    }
  }
}

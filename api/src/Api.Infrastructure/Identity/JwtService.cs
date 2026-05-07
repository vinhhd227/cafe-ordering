using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.UseCases.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Implementation of IJwtService for JWT token generation and validation.
/// Config keys: Jwt:Key, Jwt:Issuer, Jwt:Audience
/// Access token: 15 minutes (fixed)
/// </summary>
public class JwtService : IJwtService
{
  private static readonly JwtSecurityTokenHandler TokenHandler = new();

  private readonly ILogger<JwtService> _logger;
  private readonly SymmetricSecurityKey _signingKey;
  private readonly SigningCredentials _credentials;
  private readonly string _issuer;
  private readonly string _audience;
  private readonly int _expiryMinutes;

  public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
  {
    _logger = logger;

    var rawKey = configuration["Jwt:Key"]
      ?? throw new InvalidOperationException("JWT Key not configured (Jwt:Key)");

    if (Encoding.UTF8.GetByteCount(rawKey) < 32)
      throw new InvalidOperationException("Jwt:Key must be at least 256 bits (32 bytes).");

    _signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
    _credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
    _issuer      = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured (Jwt:Issuer)");
    _audience    = configuration["Jwt:Audience"] ?? _issuer;
    _expiryMinutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var m) ? m : 15;
  }

  public string GenerateAccessToken(
    Guid userId,
    string username,
    string fullName,
    IList<string> roles,
    IList<string> permissions,
    Guid? staffId = null,
    Guid? customerId = null,
    string? avatarUrl = null)
  {
    var now = DateTime.UtcNow;
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
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

    if (!string.IsNullOrEmpty(avatarUrl))
      claims.Add(new Claim("avatarUrl", avatarUrl));

    var token = new JwtSecurityToken(
      issuer: _issuer,
      audience: _audience,
      claims: claims,
      notBefore: now,
      expires: now.AddMinutes(_expiryMinutes),
      signingCredentials: _credentials);

    return TokenHandler.WriteToken(token);
  }

  public string GenerateRefreshToken()
  {
    var randomBytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);
  }

  /// <summary>
  /// Extracts claims from a token without validating its lifetime.
  /// <para>
  /// ⚠️ ONLY use for the refresh-token flow — this method intentionally bypasses expiry validation.
  /// Do NOT use for authenticating active requests (ASP.NET middleware handles that with lifetime checks).
  /// </para>
  /// Returns null if the token signature, issuer, or audience is invalid.
  /// </summary>
  public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
  {
    try
    {
      var principal = TokenHandler.ValidateToken(token, new TokenValidationParameters
      {
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _signingKey,
        ValidateIssuer = true,
        ValidIssuer = _issuer,
        ValidateAudience = true,
        ValidAudience = _audience,
        ValidateLifetime = false // intentional — used for refresh flow only
      }, out _);

      return principal;
    }
    catch (SecurityTokenException ex)
    {
      _logger.LogDebug(ex, "JWT validation failed: {Reason}", ex.Message);
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Unexpected error during JWT validation");
      return null;
    }
  }
}

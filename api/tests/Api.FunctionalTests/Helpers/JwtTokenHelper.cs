using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.FunctionalTests.Helpers;

/// <summary>
/// Generates JWT tokens for functional tests using the same key/issuer as ApiFactory.
/// </summary>
public static class JwtTokenHelper
{
  internal const string TestKey      = "test-secret-key-minimum-32-chars-long!!";
  internal const string TestIssuer   = "test-issuer";
  internal const string TestAudience = "test-audience";

  public static string ForAdmin(string userId = "admin-test-id") =>
    Generate(userId, "admin@test.com", ["Admin"]);

  public static string ForStaff(string userId = "staff-test-id") =>
    Generate(userId, "staff@test.com", ["Staff"]);

  public static string ForStaffWithPermissions(string[] permissions, string userId = "staff-test-id") =>
    Generate(userId, "staff@test.com", ["Staff"], permissions);

  private static string Generate(
    string userId,
    string email,
    string[] roles,
    string[]? permissions = null)
  {
    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, userId),
      new(ClaimTypes.Email, email),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    if (permissions is not null)
      claims.AddRange(permissions.Select(p => new Claim("permission", p)));

    var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      TestIssuer,
      TestAudience,
      claims,
      expires: DateTime.UtcNow.AddHours(1),
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}

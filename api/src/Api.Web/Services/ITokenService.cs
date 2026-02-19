using System.Security.Claims;
using Api.Infrastructure.Identity;

namespace Api.Web.Services;

public interface ITokenService
{
  Task<string> GenerateAccessTokenAsync(ApplicationUser user);
  string GenerateRefreshToken();
  ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

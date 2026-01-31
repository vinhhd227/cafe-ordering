using api.Domain.Entities;

namespace api.Infrastructure.Auth;

public interface IJwtTokenService
{
    Task<JwtTokenResult> CreateTokenAsync(ApplicationUser user);
}

public record JwtTokenResult(string AccessToken, DateTime ExpiresAt, IReadOnlyList<string> Roles);

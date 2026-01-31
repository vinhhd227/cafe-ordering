namespace api.Contracts.Auth;

public static class Login
{
    public record Request(string Email, string Password);

    public record Response(string AccessToken, DateTime ExpiresAt, IReadOnlyList<string> Roles);
}

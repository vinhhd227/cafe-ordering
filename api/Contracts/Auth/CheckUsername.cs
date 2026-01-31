namespace api.Contracts.Auth;

public static class CheckUsername
{
    public record Request(string Email);

    public record Response(bool Exists);
}

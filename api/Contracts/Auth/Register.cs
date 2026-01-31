namespace api.Contracts.Auth;

public static class Register
{
    public record Request(string Email, string Password, string FirstName, string LastName);

    public record Response(string Message);
}

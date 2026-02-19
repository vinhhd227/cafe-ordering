namespace Api.Web.Endpoints.Auth;

public class LoginSummary : Summary<LoginEndpoint>
{
  public LoginSummary()
  {
    Summary = "Authenticate and obtain JWT tokens";
    Description =
      "Validates credentials and issues a short-lived access token together with a long-lived " +
      "refresh token. The access token must be sent as a Bearer token on every authenticated request. " +
      "Each login creates a separate session row in the database, enabling simultaneous login from " +
      "multiple devices (up to 5 active sessions per user). " +
      "Failed attempts increment the lockout counter; the account is locked for 15 minutes after 5 failures.";

    ExampleRequest = new LoginRequest
    {
      Email = "john.doe@example.com",
      Password = "Secret@123",
      DeviceInfo = "Chrome/Windows"
    };

    ResponseExamples[200] = new LoginResponse
    {
      Success = true,
      Message = "Login successful",
      AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      RefreshToken = "dGhpcyBpcyBhIHNhbXBsZSByZWZyZXNoIHRva2Vu...",
      ExpiresIn = 3600,
      User = new UserDto { Id = 1, Email = "john.doe@example.com" }
    };

    Response<LoginResponse>(200, "Authentication successful. Returns an access token and a refresh token.");
    Response(400, "Request body is missing required fields.");
    Response(401, "Invalid email/password combination, or the account has been deactivated or locked.");
  }
}

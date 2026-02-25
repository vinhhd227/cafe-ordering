namespace Api.Web.Endpoints.Auth;

public class LoginSummary : Summary<LoginEndpoint>
{
  public LoginSummary()
  {
    Summary = "Authenticate and obtain JWT tokens";
    Description =
      "Validates credentials and issues a short-lived access token (15 min) together with a long-lived " +
      "refresh token (7 days). Use the access token as a Bearer token on every authenticated request. " +
      "Failed attempts increment the lockout counter; the account is locked for 15 minutes after 5 failures.";

    ExampleRequest = new LoginRequest
    {
      Username = "admin",
      Password = "Admin@123456"
    };

    ResponseExamples[200] = new LoginResponse
    {
      Success = true,
      Message = "Login successful",
      AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      RefreshToken = "dGhpcyBpcyBhIHNhbXBsZSByZWZyZXNoIHRva2Vu...",
      ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    Response<LoginResponse>(200, "Authentication successful.");
    Response(400, "Username or password missing.");
    Response(401, "Invalid credentials, or account deactivated/locked.");
  }
}

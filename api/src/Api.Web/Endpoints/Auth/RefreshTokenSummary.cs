namespace Api.Web.Endpoints.Auth;

public class RefreshTokenSummary : Summary<RefreshTokenEndpoint>
{
  public RefreshTokenSummary()
  {
    Summary = "Rotate an access token using the refresh token cookie";
    Description =
      "Issues a fresh access token without requiring the user to re-enter credentials. " +
      "Reads the refresh token from the HttpOnly cookie set at login. " +
      "Implements token rotation: the current refresh token is immediately revoked and a new one " +
      "is issued via cookie. If a previously-revoked token is presented, all active sessions for that user " +
      "are revoked as a security measure (token-theft detection).";

    ResponseExamples[200] = new RefreshTokenResponse
    {
      Success = true,
      Message = "Token refreshed successfully",
      AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...NEW",
      ExpiresAt = DateTime.UtcNow.AddMinutes(15)
    };

    Response<RefreshTokenResponse>(200, "Token rotation successful. New refresh token set as HttpOnly cookie.");
    Response(400, "Refresh token cookie is missing.");
    Response(401, "The refresh token is invalid, expired, or already revoked.");
  }
}

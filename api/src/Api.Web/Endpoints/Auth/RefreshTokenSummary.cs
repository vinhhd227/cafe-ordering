namespace Api.Web.Endpoints.Auth;

public class RefreshTokenSummary : Summary<RefreshTokenEndpoint>
{
  public RefreshTokenSummary()
  {
    Summary = "Rotate an access token using a refresh token";
    Description =
      "Issues a fresh access token without requiring the user to re-enter credentials. " +
      "Implements token rotation: the supplied refresh token is immediately revoked and a brand-new " +
      "one is returned. If a previously-revoked token is presented, all active sessions for that user " +
      "are revoked as a security measure (token-theft detection).";

    ExampleRequest = new RefreshTokenRequest
    {
      RefreshToken = "dGhpcyBpcyBhIHNhbXBsZSByZWZyZXNoIHRva2Vu..."
    };

    ResponseExamples[200] = new RefreshTokenResponse
    {
      Success = true,
      Message = "Token refreshed successfully",
      AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...NEW",
      RefreshToken = "bmV3UmVmcmVzaFRva2VuVmFsdWVIZXJl...",
      ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    Response<RefreshTokenResponse>(200, "Token rotation successful.");
    Response(400, "Refresh token field is missing.");
    Response(401, "The refresh token is invalid, expired, or already revoked.");
  }
}

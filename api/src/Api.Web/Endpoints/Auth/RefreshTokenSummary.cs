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
      "are revoked as a security measure (token-theft detection). " +
      "The access token's lifetime is reset to the configured expiry window.";

    ExampleRequest = new RefreshTokenRequest
    {
      AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      RefreshToken = "dGhpcyBpcyBhIHNhbXBsZSByZWZyZXNoIHRva2Vu..."
    };

    ResponseExamples[200] = new RefreshTokenResponse
    {
      Success = true,
      Message = "Token refreshed successfully",
      AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...NEW",
      RefreshToken = "bmV3UmVmcmVzaFRva2VuVmFsdWVIZXJl...",
      ExpiresIn = 3600
    };

    Response<RefreshTokenResponse>(200, "Token rotation successful. Both tokens in the response are fresh; store them and discard the old pair.");
    Response(400, "Request body is missing one or both token fields.");
    Response(401,
      "The refresh token is invalid, expired, or already revoked. " +
      "If a revoked token was detected, all active sessions have been terminated for security.");
  }
}

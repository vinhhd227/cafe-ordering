namespace Api.Web.Endpoints.Auth;

public class LogoutSummary : Summary<LogoutEndpoint>
{
  public LogoutSummary()
  {
    Summary = "Logout and revoke the current refresh token";
    Description =
      "Revokes the refresh token stored in the HttpOnly cookie and clears the cookie. " +
      "Safe to call even if no cookie is present (idempotent). " +
      "The access token is short-lived (15 min) and will expire naturally after logout.";

    Response(204, "Logout successful. Refresh token cookie cleared.");
    Response(401, "Not authenticated.");
  }
}

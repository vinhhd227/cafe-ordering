namespace Api.Web.Endpoints.Auth;

public class LoginAdminSummary : Summary<LoginAdminEndpoint>
{
    public LoginAdminSummary()
    {
        Summary = "Admin site login";
        Description = "Authenticate with username and password for the admin management site. " +
                      "Requires the account to have the 'admin.access' permission (Admin or Staff role). " +
                      "Issues a JWT access token and sets an HttpOnly refresh token cookie.";
        Response<LoginResponse>(200, "Login successful — access token returned, refresh token set as HttpOnly cookie");
        Response(400, "Username or password missing");
        Response(401, "Invalid credentials (username not found or wrong password)");
        Response(403, "Account inactive (account_inactive), locked (account_locked), or lacks admin.access permission (access_denied)");
    }
}

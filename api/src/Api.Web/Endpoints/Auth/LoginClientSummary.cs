namespace Api.Web.Endpoints.Auth;

public class LoginClientSummary : Summary<LoginClientEndpoint>
{
    public LoginClientSummary()
    {
        Summary = "Customer site login";
        Description = "Authenticate with username and password for the customer ordering site. " +
                      "Requires the account to have the 'customer.access' permission (Customer role). " +
                      "Issues a JWT access token and sets an HttpOnly refresh token cookie.";
        Response<LoginResponse>(200, "Login successful — access token returned, refresh token set as HttpOnly cookie");
        Response(400, "Username or password missing");
        Response(401, "Invalid credentials or account deactivated/locked");
        Response(403, "Account does not have access to the customer site");
    }
}

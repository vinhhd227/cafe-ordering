namespace Api.Web.Endpoints.Auth;

public class GetCurrentUserSummary : Summary<GetCurrentUserEndpoint>
{
  public GetCurrentUserSummary()
  {
    Summary = "Get the profile of the currently authenticated user";
    Description =
      "Returns identity details and assigned roles for the user identified by the Bearer token. " +
      "Useful for bootstrapping the client-side session after login or a page reload.";

    ResponseExamples[200] = new GetCurrentUserResponse
    {
      Success = true,
      User = new UserDto { Id = 1, Email = "john.doe@example.com" },
      Roles = ["Customer"]
    };

    Response<GetCurrentUserResponse>(200, "Returns the authenticated user's basic profile and role list.");
    Response(401, "The Bearer token is missing, invalid, or the account has been deactivated.");
  }
}

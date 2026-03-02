namespace Api.Web.Endpoints.Auth;

public class ChangePasswordSummary : Summary<ChangePasswordEndpoint>
{
  public ChangePasswordSummary()
  {
    Summary = "Change the current user's password";
    Description =
      "Allows an authenticated user to update their own password by providing the current password for " +
      "verification. The new password must meet the application's complexity requirements. " +
      "On success the endpoint returns 204 No Content with no response body. " +
      "Returns 400 if the current password is wrong or the new password fails validation, " +
      "and 401 if the caller is not authenticated.";

    ExampleRequest = new ChangePasswordRequest
    {
      CurrentPassword = "OldPassword@123",
      NewPassword     = "NewPassword@456"
    };

    Response(204, "Password changed successfully. No response body is returned.");
    Response(400, "Current password is incorrect, or the new password does not meet complexity requirements.");
    Response(401, "The Bearer token is missing, invalid, or the account has been deactivated.");
  }
}

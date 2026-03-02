namespace Api.Web.Endpoints.Auth;

public class CheckUsernameSummary : Summary<CheckUsernameEndpoint>
{
  public CheckUsernameSummary()
  {
    Summary = "Check whether a username is already registered";
    Description =
      "Publicly accessible endpoint that returns whether the given username is already taken. " +
      "Intended for real-time availability feedback on registration forms before the user submits. " +
      "Always returns 200 — the caller should inspect the 'exists' field in the response body.";

    ExampleRequest = new CheckUsernameRequest
    {
      Username = "john.doe"
    };

    ResponseExamples[200] = new CheckUsernameResponse { Exists = false };

    Response<CheckUsernameResponse>(200,
      "Returns { \"exists\": false } if the username is available, or { \"exists\": true } if it is already taken.");
    Response(400, "The username field is empty or missing.");
  }
}

namespace Api.Web.Endpoints.Auth;

public class RegisterSummary : Summary<RegisterEndpoint>
{
  public RegisterSummary()
  {
    Summary = "Register a new customer account";
    Description =
      "Creates both an identity user (for authentication) and a customer profile (for business data). " +
      "The two records are linked internally via IdentityGuid — no cross-database foreign key is used. " +
      "On success the caller receives the new customer ID which can be used in subsequent API calls.";

    ExampleRequest = new RegisterRequest
    {
      Username = "john.doe",
      Email = "john.doe@example.com",
      Password = "Secret@123",
      FullName = "John Doe"
    };

    ResponseExamples[200] = new RegisterResponse
    {
      CustomerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      Email = "john.doe@example.com"
    };

    Response<RegisterResponse>(200, "Registration successful. Returns the new customer ID and confirmed email.");
    Response<ErrorResponse>(400, "Validation failed (e.g. weak password) or the email address is already registered.");
  }
}

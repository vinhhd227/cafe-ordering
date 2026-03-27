namespace Api.Web.Endpoints.Auth;

public class RegisterClientSummary : Summary<RegisterClientEndpoint>
{
    public RegisterClientSummary()
    {
        Summary = "Customer self-registration";
        Description = "Register a new customer account for the ordering site. " +
                      "Creates an Identity user with the Customer role and a linked Customer aggregate. " +
                      "Admin/Staff accounts cannot be self-registered — use the staff management endpoints instead.";
        Response<RegisterResponse>(200, "Registration successful — CustomerId and email returned");
        Response(400, "Validation failed (username taken, weak password, etc.)");
    }
}

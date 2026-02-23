namespace Api.Web.Endpoints.Sessions;

public class MergeWithCustomerSummary : Summary<MergeWithCustomer>
{
  public MergeWithCustomerSummary()
  {
    Summary = "Merge a guest session with a customer account";
    Description =
      "Associates the guest session with a registered customer, typically called after " +
      "a guest logs in mid-session. Requires authentication. " +
      "The session must be active to be merged.";

    Params["SessionId"] = "The GUID of the guest session to merge.";

    Response(200, "Session merged with customer successfully.");
    Response(404, "Session or customer not found.");
    Response(409, "Session is already closed.");
    Response(401, "Authentication required.");
  }
}

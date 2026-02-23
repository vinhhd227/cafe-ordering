namespace Api.Web.Endpoints.Sessions;

public class CloseSessionSummary : Summary<CloseSession>
{
  public CloseSessionSummary()
  {
    Summary = "Close an active guest session";
    Description =
      "Closes the specified guest session and sets the table status to Cleaning. " +
      "Requires Barista, Manager, or Admin role. " +
      "After closing the session, use PUT /tables/{tableId}/available when the table is ready.";

    Params["SessionId"] = "The GUID of the session to close.";

    Response(200, "Session closed successfully.");
    Response(404, "No session with the given ID was found.");
    Response(409, "Session is already closed.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient role permissions.");
  }
}

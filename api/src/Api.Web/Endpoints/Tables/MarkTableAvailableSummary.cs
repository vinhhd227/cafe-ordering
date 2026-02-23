namespace Api.Web.Endpoints.Tables;

public class MarkTableAvailableSummary : Summary<MarkTableAvailable>
{
  public MarkTableAvailableSummary()
  {
    Summary = "Mark a table as available after cleaning";
    Description =
      "Sets the table status from Cleaning to Available, indicating the table is ready for " +
      "new guests. Requires Barista, Manager, or Admin role. " +
      "Typically called after closing a session and finishing clean-up.";

    Params["TableId"] = "The integer ID of the table.";

    Response(200, "Table marked as available successfully.");
    Response(404, "No table with the given ID was found.");
    Response(409, "Table has an active session — close the session first.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient role permissions.");
  }
}

namespace Api.Web.Endpoints.Tables;

public class ActivateTableSummary : Summary<ActivateTable>
{
  public ActivateTableSummary()
  {
    Summary = "Activate a table";
    Description =
      "Marks the specified table as active, making it visible and selectable for new orders. " +
      "This operation is idempotent — activating an already-active table returns 200 with no side effects. " +
      "Requires the table.update permission.";

    Params["TableId"] = "The integer ID of the table to activate.";

    Response(200, "Table activated successfully.");
    Response(404, "No table with the given ID was found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.update).");
  }
}

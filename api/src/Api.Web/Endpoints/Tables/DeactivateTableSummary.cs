namespace Api.Web.Endpoints.Tables;

public class DeactivateTableSummary : Summary<DeactivateTable>
{
  public DeactivateTableSummary()
  {
    Summary = "Deactivate a table";
    Description =
      "Marks the specified table as inactive, hiding it from the customer-facing menu and preventing new orders. " +
      "Cannot deactivate a table that currently has an active session (Occupied status). " +
      "This operation is idempotent — deactivating an already-inactive table returns 200 with no side effects. " +
      "Requires the table.update permission.";

    Params["TableId"] = "The integer ID of the table to deactivate.";

    Response(200, "Table deactivated successfully.");
    Response(404, "No table with the given ID was found.");
    Response(409, "Table has an active session and cannot be deactivated.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.update).");
  }
}

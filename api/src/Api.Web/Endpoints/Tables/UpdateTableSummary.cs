namespace Api.Web.Endpoints.Tables;

public class UpdateTableSummary : Summary<UpdateTable>
{
  public UpdateTableSummary()
  {
    Summary = "Update a table's code";
    Description =
      "Updates the code of an existing table. The new code must be unique across all tables. " +
      "Requires the table.update permission.";

    Params["TableId"] = "The integer ID of the table to update.";

    Response(200, "Table updated successfully.");
    Response(400, "Validation failed — code is empty or already used by another table.");
    Response(404, "No table with the given ID was found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.update).");
  }
}

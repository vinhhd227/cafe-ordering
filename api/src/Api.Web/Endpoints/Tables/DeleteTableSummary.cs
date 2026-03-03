namespace Api.Web.Endpoints.Tables;

public class DeleteTableSummary : Summary<DeleteTable>
{
  public DeleteTableSummary()
  {
    Summary = "Soft-delete a table";
    Description =
      "Marks a table as deleted (soft delete). The table will no longer appear in listings " +
      "and cannot be used for new sessions. This action records the deleting user for audit purposes. " +
      "Requires the table.delete permission.";

    Params["TableId"] = "The integer ID of the table to delete.";

    Response(204, "Table deleted successfully.");
    Response(404, "No table with the given ID was found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.delete).");
  }
}

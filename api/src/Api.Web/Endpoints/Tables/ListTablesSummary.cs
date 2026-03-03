namespace Api.Web.Endpoints.Tables;

public class ListTablesSummary : Summary<ListTables>
{
  public ListTablesSummary()
  {
    Summary = "List all tables";
    Description =
      "Returns all tables including their current status (Available, Occupied, Cleaning) " +
      "and active flag. Soft-deleted tables are excluded. " +
      "Requires the table.read permission.";

    Response(200, "List of tables returned successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.read).");
  }
}

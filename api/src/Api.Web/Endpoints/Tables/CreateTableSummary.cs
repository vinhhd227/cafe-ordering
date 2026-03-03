namespace Api.Web.Endpoints.Tables;

public class CreateTableSummary : Summary<CreateTable>
{
  public CreateTableSummary()
  {
    Summary = "Create a new table";
    Description =
      "Creates a new table with the given code. The code must be unique across all tables. " +
      "The table is created in the Available status and marked as active. " +
      "Requires the table.create permission.";

    Response(200, "Table created successfully.");
    Response(400, "Validation failed — code is empty or already in use.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.create).");
  }
}

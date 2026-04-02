namespace Api.Web.Endpoints.Expenses;

public class GetItemNamesSummary : Summary<GetItemNamesEndpoint>
{
  public GetItemNamesSummary()
  {
    Summary = "Get expense item name suggestions";
    Description =
      "Returns up to 20 distinct expense item names matching the search query. " +
      "Used for autocomplete when creating or editing an expense. Requires expense.read permission.";

    Params["Q"] = "Optional. Search string to filter item names (case-insensitive contains).";

    Response<List<string>>(200, "List of matching item name suggestions.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

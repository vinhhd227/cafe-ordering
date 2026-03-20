using Api.UseCases.Tables.DTOs;

namespace Api.Web.Endpoints.Tables;

public class ListPublicTablesSummary : Summary<ListPublicTables>
{
  public ListPublicTablesSummary()
  {
    Summary = "List all active tables (public)";
    Description = "Returns all active, non-deleted tables. No authentication required. Used by the client ordering app for table selection.";

    ResponseExamples[200] = new List<PublicTableDto>
    {
      new(1, "T01", "Available"),
      new(2, "T02", "Occupied"),
    };

    Response<List<PublicTableDto>>(200, "List of active tables with current status.");
  }
}

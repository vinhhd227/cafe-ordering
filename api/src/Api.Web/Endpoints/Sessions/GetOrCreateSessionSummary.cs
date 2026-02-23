using Api.Core.Aggregates.GuestSessionAggregate;
using Api.UseCases.Sessions.DTOs;

namespace Api.Web.Endpoints.Sessions;

public class GetOrCreateSessionSummary : Summary<GetOrCreateSession>
{
  public GetOrCreateSessionSummary()
  {
    Summary = "Get or create a guest session for a table";
    Description =
      "Returns the active guest session for the given table. " +
      "If no active session exists, a new one is created and the table status is set to Occupied. " +
      "This endpoint is idempotent — repeated calls return the same session until it is closed.";

    Params["TableId"] = "The integer ID of the table.";

    ResponseExamples[200] = new SessionContextDto(
      SessionId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
      TableId: 1,
      Status: GuestSessionStatus.Active);

    Response<SessionContextDto>(200, "Returns the active or newly created session context.");
    Response(404, "No table with the given ID was found or the table is deleted.");
    Response(400, "Table is inactive.");
  }
}

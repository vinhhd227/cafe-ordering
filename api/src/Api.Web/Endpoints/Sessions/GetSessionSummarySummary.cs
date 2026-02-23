using Api.Core.Aggregates.GuestSessionAggregate;
using Api.UseCases.Sessions.DTOs;

namespace Api.Web.Endpoints.Sessions;

public class GetSessionSummarySummary : Summary<GetSessionSummary>
{
  public GetSessionSummarySummary()
  {
    Summary = "Get session summary with all orders";
    Description =
      "Returns the guest session detail along with all orders placed in the session " +
      "and the computed grand total.";

    Params["SessionId"] = "The GUID of the guest session.";

    ResponseExamples[200] = new SessionSummaryDto(
      SessionId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
      TableId: 1,
      OpenedAt: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
      Status: GuestSessionStatus.Active,
      Orders: [],
      GrandTotal: 0m);

    Response<SessionSummaryDto>(200, "Returns the session summary.");
    Response(404, "No session with the given ID was found.");
  }
}

using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Reports;

public class GetDailyOrdersSummary : Summary<GetDailyOrders>
{
  public GetDailyOrdersSummary()
  {
    Summary = "Get order list for a specific date";
    Description =
      "Returns the full list of orders placed on the given date, with item snapshots and resolved " +
      "table numbers. Used for the orders detail table in the daily report screen. " +
      "Requires report.read permission.";

    ExampleRequest = new GetDailyOrdersRequest
    {
      Date = new DateOnly(2026, 2, 5),
    };

    Response<DailyOrdersResponseDto>(200, "List of orders for the requested date.");
    Response(400, "Invalid date format.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Reports;

public class GetOrdersSummarySummary : Summary<GetOrdersSummary>
{
  public GetOrdersSummarySummary()
  {
    Summary = "Get orders summary for a date range";
    Description =
      "Returns aggregated order statistics (revenue, tips, order counts by status) for a given date range. " +
      "DateFrom and DateTo are optional; omit to query all time. Requires Admin role.";

    ExampleRequest = new GetOrdersSummaryRequest
    {
      DateFrom = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
      DateTo = new DateTime(2026, 2, 7, 23, 59, 59, DateTimeKind.Utc),
    };

    Response<OrdersSummaryDto>(200, "Aggregated summary for the selected period.", example: new OrdersSummaryDto(
      CashRevenue: 1500000,
      BankRevenue: 800000,
      TotalRevenue: 2300000,
      TotalTips: 150000,
      TotalOrders: 42,
      CompletedOrders: 38,
      CancelledOrders: 2,
      PendingOrders: 1,
      ProcessingOrders: 1,
      TotalItemsSold: 95,
      DailyRevenue: []
    ));
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions — Admin role required.");
  }
}

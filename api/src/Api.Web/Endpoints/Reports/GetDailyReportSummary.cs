using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Reports;

public class GetDailyReportSummary : Summary<GetDailyReport>
{
  public GetDailyReportSummary()
  {
    Summary = "Get daily report for a specific date";
    Description =
      "Returns aggregated daily statistics including revenue by payment method, order counts, " +
      "averages, peak hour, top 5 products, top categories with trend, and hourly revenue breakdown " +
      "(including 7-day hourly average). Requires report.read permission.";

    ExampleRequest = new GetDailyReportRequest
    {
      Date = new DateOnly(2026, 2, 5),
    };

    Response<DailyReportDto>(200, "Daily report for the requested date.");
    Response(400, "Invalid date format.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

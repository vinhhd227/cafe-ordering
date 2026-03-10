using Api.UseCases.Expenses.DTOs;

namespace Api.Web.Endpoints.Expenses;

public class GetExpenseSummarySummary : Summary<GetExpenseSummaryEndpoint>
{
  public GetExpenseSummarySummary()
  {
    Summary = "Get P&L summary: revenue vs expenses";
    Description =
      "Returns aggregated revenue (from PAID orders) and expenses (by category) for a given period. " +
      "Profit = Revenue.Total - Expenses.Total. Used for the P&L dashboard. Requires Staff or Admin.";

    Params["DateFrom"] = "Optional. Include data on or after this date (ISO 8601).";
    Params["DateTo"]   = "Optional. Include data on or before this date (ISO 8601).";

    Response<ExpenseSummaryDto>(200, "P&L summary returned successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

using Api.UseCases.Expenses.DTOs;

namespace Api.Web.Endpoints.Expenses;

public class ListSummary : Summary<ListExpenses>
{
  public ListSummary()
  {
    Summary = "List expense records with pagination";
    Description =
      "Returns a paginated list of expense records, optionally filtered by category and date range. " +
      "Results are ordered by purchase date descending. Requires Staff or Admin.";

    Params["Category"]      = "Optional. Filter by expense category: INGREDIENT, SUPPLY, EQUIPMENT, OTHER.";
    Params["PaymentMethod"] = "Optional. Filter by payment method: CASH, BANK_TRANSFER.";
    Params["DateFrom"]      = "Optional. Include expenses on or after this date (ISO 8601).";
    Params["DateTo"]        = "Optional. Include expenses on or before this date (ISO 8601).";
    Params["Page"]          = "Page number (1-based). Default: 1.";
    Params["PageSize"]      = "Number of items per page. Default: 20.";

    Response<PagedExpensesDto>(200, "Paginated list of expenses.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

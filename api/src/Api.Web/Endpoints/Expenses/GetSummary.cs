using Api.UseCases.Expenses.DTOs;

namespace Api.Web.Endpoints.Expenses;

public class GetSummary : Summary<GetExpense>
{
  public GetSummary()
  {
    Summary = "Get a single expense record by ID";
    Description = "Returns full details for one expense record. Requires Staff or Admin.";

    Params["Id"] = "The integer ID of the expense to retrieve.";

    Response<ExpenseDto>(200, "Expense record returned.");
    Response(404, "No expense with the given ID found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

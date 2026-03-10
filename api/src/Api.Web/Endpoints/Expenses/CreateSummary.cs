using Api.UseCases.Expenses.DTOs;

namespace Api.Web.Endpoints.Expenses;

public class CreateSummary : Summary<CreateExpense>
{
  public CreateSummary()
  {
    Summary = "Create a new expense record";
    Description =
      "Records a new expense item (ingredient, supply, equipment, or other cost). " +
      "TotalAmount is automatically computed as Quantity × UnitPrice. Requires Staff or Admin.";

    Response<ExpenseDto>(201, "Expense created successfully.");
    Response(400, "Validation failed (missing name, invalid category, negative quantity, etc.).");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

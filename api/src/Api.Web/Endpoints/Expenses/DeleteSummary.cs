namespace Api.Web.Endpoints.Expenses;

public class DeleteSummary : Summary<DeleteExpense>
{
  public DeleteSummary()
  {
    Summary = "Soft-delete an expense record";
    Description =
      "Marks an expense as deleted without removing it from the database. " +
      "Deleted expenses are excluded from all list and summary queries. Requires Staff or Admin.";

    Params["Id"] = "The integer ID of the expense to delete.";

    Response(200, "Expense deleted successfully.");
    Response(404, "No expense with the given ID found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

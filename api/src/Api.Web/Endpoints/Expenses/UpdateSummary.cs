namespace Api.Web.Endpoints.Expenses;

public class UpdateSummary : Summary<UpdateExpense>
{
  public UpdateSummary()
  {
    Summary = "Update an existing expense record";
    Description =
      "Replaces all editable fields of an expense record. " +
      "TotalAmount is recomputed as Quantity × UnitPrice. Requires Staff or Admin.";

    Params["Id"] = "The integer ID of the expense to update.";

    Response(200, "Expense updated successfully.");
    Response(400, "Validation failed.");
    Response(404, "No expense with the given ID found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

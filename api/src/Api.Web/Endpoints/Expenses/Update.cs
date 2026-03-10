using Api.UseCases.Expenses.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class UpdateExpenseRequest
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string PaymentMethod { get; set; } = "CASH";
  public decimal Quantity { get; set; }
  public string? Unit { get; set; }
  public decimal UnitPrice { get; set; }
  public DateTime PurchaseDate { get; set; }
  public string? Notes { get; set; }
}

public class UpdateExpense(IMediator mediator) : Endpoint<UpdateExpenseRequest>
{
  public override void Configure()
  {
    Put("/api/admin/expenses/{Id}");
    Policies("expense.update");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(UpdateExpenseRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new UpdateExpenseCommand(
        req.Id, req.Name, req.Category, req.PaymentMethod, req.Quantity, req.Unit,
        req.UnitPrice, req.PurchaseDate, req.Notes), ct);

    await this.SendResultAsync(result, ct);
  }
}

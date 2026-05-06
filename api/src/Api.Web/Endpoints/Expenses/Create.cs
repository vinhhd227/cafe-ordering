using Api.UseCases.Expenses.Create;
using Api.UseCases.Expenses.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class CreateExpenseRequest
{
  public string Name { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string PaymentMethod { get; set; } = "CASH";
  public decimal Quantity { get; set; }
  public string? Unit { get; set; }
  public decimal UnitPrice { get; set; }
  public int? TotalAmount { get; set; }
  public DateTime PurchaseDate { get; set; }
  public string? Notes { get; set; }
}

public class CreateExpense(IMediator mediator) : Endpoint<CreateExpenseRequest, ExpenseDto>
{
  public override void Configure()
  {
    Post("/api/admin/expenses");
    Policies("expense.create");
    Policies("expense.create");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(CreateExpenseRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new CreateExpenseCommand(
        req.Name, req.Category, req.PaymentMethod, req.Quantity, req.Unit,
        req.UnitPrice, req.PurchaseDate, req.Notes, req.TotalAmount), ct);

    await this.SendResultAsync(result, ct);
  }
}

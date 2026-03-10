using Api.UseCases.Expenses.DTOs;
using Api.UseCases.Expenses.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class GetExpenseRequest
{
  public int Id { get; set; }
}

public class GetExpense(IMediator mediator) : Endpoint<GetExpenseRequest, ExpenseDto>
{
  public override void Configure()
  {
    Get("/api/admin/expenses/{Id}");
    Policies("expense.read");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(GetExpenseRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetExpenseQuery(req.Id), ct);

    await this.SendResultAsync(result, ct);
  }
}

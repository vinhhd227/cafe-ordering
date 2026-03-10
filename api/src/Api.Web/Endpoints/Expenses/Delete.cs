using System.Security.Claims;
using Api.UseCases.Expenses.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class DeleteExpenseRequest
{
  public int Id { get; set; }
}

public class DeleteExpense(IMediator mediator) : Endpoint<DeleteExpenseRequest>
{
  public override void Configure()
  {
    Delete("/api/admin/expenses/{Id}");
    Policies("expense.delete");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(DeleteExpenseRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";

    var result = await mediator.Send(new DeleteExpenseCommand(req.Id, deletedBy), ct);

    await this.SendResultAsync(result, ct);
  }
}

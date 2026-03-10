using Api.UseCases.Expenses.DTOs;
using Api.UseCases.Expenses.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class ListExpensesRequest
{
  [QueryParam] public string?   Category  { get; set; }
  [QueryParam] public DateTime? DateFrom  { get; set; }
  [QueryParam] public DateTime? DateTo    { get; set; }
  [QueryParam] public int       Page      { get; set; } = 1;
  [QueryParam] public int       PageSize  { get; set; } = 20;
}

public class ListExpenses(IMediator mediator) : Endpoint<ListExpensesRequest, PagedExpensesDto>
{
  public override void Configure()
  {
    Get("/api/admin/expenses");
    Policies("expense.read");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(ListExpensesRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ListExpensesQuery(req.Category, req.DateFrom, req.DateTo, req.Page, req.PageSize), ct);

    await this.SendResultAsync(result, ct);
  }
}

using Api.UseCases.Expenses.DTOs;
using Api.UseCases.Expenses.GetSummary;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class GetExpenseSummaryRequest
{
  [QueryParam] public DateTime? DateFrom { get; set; }
  [QueryParam] public DateTime? DateTo   { get; set; }
}

public class GetExpenseSummaryEndpoint(IMediator mediator)
  : Endpoint<GetExpenseSummaryRequest, ExpenseSummaryDto>
{
  public override void Configure()
  {
    Get("/api/admin/expenses/summary");
    Policies("expense.read");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(GetExpenseSummaryRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new GetExpenseSummaryQuery(req.DateFrom, req.DateTo), ct);

    await this.SendResultAsync(result, ct);
  }
}

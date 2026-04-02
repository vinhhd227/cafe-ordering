using Api.UseCases.Expenses.GetItemNames;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Expenses;

public sealed class GetItemNamesRequest
{
  [QueryParam] public string? Q { get; set; }
}

public class GetItemNamesEndpoint(IMediator mediator)
  : Endpoint<GetItemNamesRequest, List<string>>
{
  public override void Configure()
  {
    Get("/api/admin/expenses/item-names");
    Policies("expense.read");
    DontAutoTag();
    Description(b => b.WithTags("Expenses"));
  }

  public override async Task HandleAsync(GetItemNamesRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetItemNamesQuery(req.Q), ct);
    await this.SendResultAsync(result, ct);
  }
}

using Api.UseCases.Tables.Activate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class ActivateTableRequest
{
  public int TableId { get; set; }
}

public class ActivateTable(IMediator mediator) : Endpoint<ActivateTableRequest>
{
  public override void Configure()
  {
    Put("/api/admin/tables/{TableId}/activate");
    Policies("table.update");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(ActivateTableRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ActivateTableCommand(req.TableId), ct);
    await this.SendResultAsync(result, ct);
  }
}

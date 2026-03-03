using Api.UseCases.Tables.Deactivate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class DeactivateTableRequest
{
  public int TableId { get; set; }
}

public class DeactivateTable(IMediator mediator) : Endpoint<DeactivateTableRequest>
{
  public override void Configure()
  {
    Put("/api/admin/tables/{TableId}/deactivate");
    Policies("table.update");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(DeactivateTableRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new DeactivateTableCommand(req.TableId), ct);
    await this.SendResultAsync(result, ct);
  }
}

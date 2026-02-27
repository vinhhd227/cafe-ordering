using Api.UseCases.Tables.ToggleActive;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class ToggleTableActiveRequest
{
  public int TableId { get; set; }
}

public class ToggleTableActive(IMediator mediator) : Endpoint<ToggleTableActiveRequest>
{
  public override void Configure()
  {
    Patch("/api/admin/tables/{TableId}/toggle-active");
    Policies("table.update");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(ToggleTableActiveRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ToggleTableActiveCommand(req.TableId), ct);
    await this.SendResultAsync(result, ct);
  }
}

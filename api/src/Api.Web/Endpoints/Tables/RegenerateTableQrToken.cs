using Api.UseCases.Tables.DTOs;
using Api.UseCases.Tables.RegenerateQrToken;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class RegenerateTableQrTokenRequest
{
  public int TableId { get; set; }
}

public class RegenerateTableQrToken(IMediator mediator)
  : Endpoint<RegenerateTableQrTokenRequest, TableDto>
{
  public override void Configure()
  {
    Put("/api/admin/tables/{TableId}/qr-token");
    Policies("table.update");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(RegenerateTableQrTokenRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new RegenerateQrTokenCommand(req.TableId), ct);
    await this.SendResultAsync(result, ct);
  }
}

using Api.UseCases.Zones.Activate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public sealed class ActivateZoneRequest
{
  public int ZoneId { get; set; }
}

public class ActivateZone(IMediator mediator) : Ep.Req<ActivateZoneRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/zones/{ZoneId}/activate");
    Policies("zone.update");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(ActivateZoneRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ActivateZoneCommand(req.ZoneId), ct);
    await this.SendResultAsync(result, ct);
  }
}

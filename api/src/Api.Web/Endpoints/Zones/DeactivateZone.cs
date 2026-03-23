using Api.UseCases.Zones.Deactivate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public sealed class DeactivateZoneRequest
{
  public int ZoneId { get; set; }
}

public class DeactivateZone(IMediator mediator) : Ep.Req<DeactivateZoneRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/zones/{ZoneId}/deactivate");
    Policies("zone.update");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(DeactivateZoneRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new DeactivateZoneCommand(req.ZoneId), ct);
    await this.SendResultAsync(result, ct);
  }
}

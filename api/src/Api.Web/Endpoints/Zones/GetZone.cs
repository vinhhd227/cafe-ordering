using Api.UseCases.Zones.DTOs;
using Api.UseCases.Zones.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public sealed class GetZoneRequest
{
  public int ZoneId { get; set; }
}

public class GetZone(IMediator mediator) : Endpoint<GetZoneRequest, ZoneDto>
{
  public override void Configure()
  {
    Get("/api/admin/zones/{ZoneId}");
    Policies("zone.read");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(GetZoneRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetZoneQuery(req.ZoneId), ct);
    await this.SendResultAsync(result, ct);
  }
}

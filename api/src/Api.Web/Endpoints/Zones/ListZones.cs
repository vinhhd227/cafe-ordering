using Api.UseCases.Zones.DTOs;
using Api.UseCases.Zones.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public class ListZones(IMediator mediator) : EndpointWithoutRequest<List<ZoneDto>>
{
  public override void Configure()
  {
    Get("/api/admin/zones");
    Policies("zone.read");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListZonesQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}

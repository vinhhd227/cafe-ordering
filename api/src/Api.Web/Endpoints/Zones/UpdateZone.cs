using Api.UseCases.Zones.DTOs;
using Api.UseCases.Zones.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public sealed class UpdateZoneRequest
{
  public int ZoneId { get; set; }
  public string Name { get; set; } = string.Empty;
  public int DisplayOrder { get; set; }
}

public class UpdateZone(IMediator mediator) : Endpoint<UpdateZoneRequest, ZoneDto>
{
  public override void Configure()
  {
    Put("/api/admin/zones/{ZoneId}");
    Policies("zone.update");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(UpdateZoneRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdateZoneCommand(req.ZoneId, req.Name, req.DisplayOrder), ct);
    await this.SendResultAsync(result, ct);
  }
}

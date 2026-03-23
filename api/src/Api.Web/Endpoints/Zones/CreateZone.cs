using Api.UseCases.Zones.Create;
using Api.UseCases.Zones.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public sealed class CreateZoneRequest
{
  public string Name { get; set; } = string.Empty;
  public int DisplayOrder { get; set; }
}

public class CreateZone(IMediator mediator) : Endpoint<CreateZoneRequest, ZoneDto>
{
  public override void Configure()
  {
    Post("/api/admin/zones");
    Policies("zone.create");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(CreateZoneRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CreateZoneCommand(req.Name, req.DisplayOrder), ct);
    await this.SendResultAsync(result, ct);
  }
}

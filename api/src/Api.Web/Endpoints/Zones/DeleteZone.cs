using System.Security.Claims;
using Api.UseCases.Zones.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Zones;

public sealed class DeleteZoneRequest
{
  public int ZoneId { get; set; }
}

public class DeleteZone(IMediator mediator) : Ep.Req<DeleteZoneRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/zones/{ZoneId}");
    Policies("zone.delete");
    DontAutoTag();
    Description(b => b.WithTags("Zones"));
  }

  public override async Task HandleAsync(DeleteZoneRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeleteZoneCommand(req.ZoneId, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}

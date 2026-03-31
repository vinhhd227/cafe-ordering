using System.Security.Claims;
using Api.UseCases.WifiProfiles.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.WifiProfiles;

public sealed class DeleteWifiProfileRequest
{
  public int Id { get; set; }
}

public class DeleteWifiProfile(IMediator mediator) : Ep.Req<DeleteWifiProfileRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/wifi-profiles/{Id}");
    Policies("utility.delete");
    DontAutoTag();
    Description(b => b.WithTags("WiFi Profiles"));
  }

  public override async Task HandleAsync(DeleteWifiProfileRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeleteWifiProfileCommand(req.Id, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}

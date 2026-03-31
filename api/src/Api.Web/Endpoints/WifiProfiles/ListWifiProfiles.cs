using Api.UseCases.WifiProfiles.DTOs;
using Api.UseCases.WifiProfiles.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.WifiProfiles;

public class ListWifiProfiles(IMediator mediator) : EndpointWithoutRequest<List<WifiProfileDto>>
{
  public override void Configure()
  {
    Get("/api/admin/wifi-profiles");
    Policies("utility.read");
    DontAutoTag();
    Description(b => b.WithTags("WiFi Profiles"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListWifiProfilesQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}

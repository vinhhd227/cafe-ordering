using Api.UseCases.WifiProfiles.Create;
using Api.UseCases.WifiProfiles.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.WifiProfiles;

public sealed class CreateWifiProfileRequest
{
  public string Name { get; set; } = string.Empty;
  public string Ssid { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string SecurityType { get; set; } = "WPA";
  public string ForegroundColor { get; set; } = "#000000";
  public string BackgroundColor { get; set; } = "#ffffff";
  public string? CustomText { get; set; }
}

public class CreateWifiProfile(IMediator mediator) : Endpoint<CreateWifiProfileRequest, WifiProfileDto>
{
  public override void Configure()
  {
    Post("/api/admin/wifi-profiles");
    Policies("utility.create");
    DontAutoTag();
    Description(b => b.WithTags("WiFi Profiles"));
  }

  public override async Task HandleAsync(CreateWifiProfileRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CreateWifiProfileCommand(
      req.Name,
      req.Ssid,
      req.Password,
      req.SecurityType,
      req.ForegroundColor,
      req.BackgroundColor,
      req.CustomText), ct);

    await this.SendResultAsync(result, ct);
  }
}

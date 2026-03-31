using Api.UseCases.WifiProfiles.DTOs;

namespace Api.Web.Endpoints.WifiProfiles;

public class CreateWifiProfileSummary : Summary<CreateWifiProfile>
{
  public CreateWifiProfileSummary()
  {
    Summary = "Save a WiFi QR profile";
    Description = "Saves a WiFi QR configuration (SSID, password, colors, custom text) as a named profile for quick reuse. Requires utility.read permission.";
    Response<WifiProfileDto>(200, "Profile saved successfully.");
    Response(400, "Validation failed — name or SSID is missing.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires utility.create).");
  }
}

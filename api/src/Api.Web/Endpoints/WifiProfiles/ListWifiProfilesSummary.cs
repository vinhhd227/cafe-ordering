using Api.UseCases.WifiProfiles.DTOs;

namespace Api.Web.Endpoints.WifiProfiles;

public class ListWifiProfilesSummary : Summary<ListWifiProfiles>
{
  public ListWifiProfilesSummary()
  {
    Summary = "List all WiFi profiles";
    Description = "Returns all saved WiFi QR profiles ordered by name. Requires utility.read permission.";
    Response<List<WifiProfileDto>>(200, "List of WiFi profiles.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires utility.read).");
  }
}

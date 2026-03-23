namespace Api.Web.Endpoints.Zones;

public class ActivateZoneSummary : Summary<ActivateZone>
{
  public ActivateZoneSummary()
  {
    Summary = "Activate a zone";
    Description =
      "Sets a zone to active status. Active zones appear in table assignment dropdowns. " +
      "Requires the zone.update permission.";

    Response(200, "Zone activated successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires zone.update).");
    Response(404, "Zone not found.");
  }
}

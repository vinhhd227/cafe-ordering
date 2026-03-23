namespace Api.Web.Endpoints.Zones;

public class DeactivateZoneSummary : Summary<DeactivateZone>
{
  public DeactivateZoneSummary()
  {
    Summary = "Deactivate a zone";
    Description =
      "Sets a zone to inactive status. Inactive zones are hidden from table assignment dropdowns " +
      "but existing table assignments are preserved. " +
      "Requires the zone.update permission.";

    Response(200, "Zone deactivated successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires zone.update).");
    Response(404, "Zone not found.");
  }
}

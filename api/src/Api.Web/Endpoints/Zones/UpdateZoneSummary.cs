namespace Api.Web.Endpoints.Zones;

public class UpdateZoneSummary : Summary<UpdateZone>
{
  public UpdateZoneSummary()
  {
    Summary = "Update a zone";
    Description =
      "Updates the name and display order of an existing zone. " +
      "The new name must be unique among non-deleted zones. " +
      "Requires the zone.update permission.";

    Response(200, "Zone updated successfully.");
    Response(400, "Validation failed — name is empty or already used by another zone.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires zone.update).");
    Response(404, "Zone not found.");
  }
}

namespace Api.Web.Endpoints.Zones;

public class CreateZoneSummary : Summary<CreateZone>
{
  public CreateZoneSummary()
  {
    Summary = "Create a new zone";
    Description =
      "Creates a new zone with the given name and display order. " +
      "Zone names must be unique across all non-deleted zones. " +
      "Requires the zone.create permission.";

    Response(200, "Zone created successfully.");
    Response(400, "Validation failed — name is empty or already exists.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires zone.create).");
  }
}

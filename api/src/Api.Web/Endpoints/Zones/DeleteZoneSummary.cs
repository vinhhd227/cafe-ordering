namespace Api.Web.Endpoints.Zones;

public class DeleteZoneSummary : Summary<DeleteZone>
{
  public DeleteZoneSummary()
  {
    Summary = "Delete a zone";
    Description =
      "Soft-deletes a zone. The zone cannot be deleted if it still has tables assigned — " +
      "reassign or remove the tables first. " +
      "Requires the zone.delete permission.";

    Response(204, "Zone deleted successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires zone.delete).");
    Response(404, "Zone not found.");
    Response(409, "Zone has tables assigned — cannot delete.");
  }
}

namespace Api.Web.Endpoints.Zones;

public class ListZonesSummary : Summary<ListZones>
{
  public ListZonesSummary()
  {
    Summary = "List all zones";
    Description =
      "Returns all non-deleted zones ordered by DisplayOrder then Name. " +
      "Each zone includes a TableCount showing how many active tables are assigned. " +
      "Requires the zone.read permission.";

    Response<List<global::Api.UseCases.Zones.DTOs.ZoneDto>>(200, "List of zones.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires zone.read).");
  }
}

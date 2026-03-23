using Api.UseCases.Zones.DTOs;

namespace Api.Web.Endpoints.Zones;

public class GetZoneSummary : Summary<GetZone>
{
  public GetZoneSummary()
  {
    Summary = "Get a zone by ID";
    Description = "Returns details of a single zone including its table count. Requires zone.read permission.";

    ResponseExamples[200] = new ZoneDto(1, "Tầng 1", 0, true, 5);

    Response<ZoneDto>(200, "Zone found.");
    Response(404, "Zone not found.");
  }
}

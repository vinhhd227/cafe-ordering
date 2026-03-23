using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;
using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.Get;

public class GetZoneHandler(
  IReadRepositoryBase<Zone> zoneRepo,
  IReadRepositoryBase<Table> tableRepo)
  : IQueryHandler<GetZoneQuery, Result<ZoneDto>>
{
  public async ValueTask<Result<ZoneDto>> Handle(GetZoneQuery request, CancellationToken ct)
  {
    var zones = await zoneRepo.ListAsync(new ZoneByIdSpec(request.ZoneId), ct);
    var zone = zones.FirstOrDefault();

    if (zone is null)
      return Result.NotFound($"Zone {request.ZoneId} not found.");

    var tables = await tableRepo.ListAsync(new TablesByZoneIdSpec(request.ZoneId), ct);

    return Result.Success(new ZoneDto(
      zone.Id,
      zone.Name,
      zone.DisplayOrder,
      zone.IsActive,
      tables.Count));
  }
}

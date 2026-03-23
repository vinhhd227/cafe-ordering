using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;
using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.List;

public class ListZonesHandler(
  IReadRepositoryBase<Zone> zoneRepo,
  IReadRepositoryBase<Table> tableRepo)
  : IQueryHandler<ListZonesQuery, Result<List<ZoneDto>>>
{
  public async ValueTask<Result<List<ZoneDto>>> Handle(ListZonesQuery request, CancellationToken ct)
  {
    var zones = await zoneRepo.ListAsync(new AllZonesSpec(), ct);
    var tables = await tableRepo.ListAsync(new AllTablesSpec(), ct);

    var tableCountByZone = tables
      .Where(t => t.ZoneId.HasValue)
      .GroupBy(t => t.ZoneId!.Value)
      .ToDictionary(g => g.Key, g => g.Count());

    var dtos = zones.Select(z => new ZoneDto(
      z.Id,
      z.Name,
      z.DisplayOrder,
      z.IsActive,
      tableCountByZone.GetValueOrDefault(z.Id, 0)
    )).ToList();

    return Result.Success(dtos);
  }
}

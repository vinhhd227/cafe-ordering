using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;
using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.Update;

public class UpdateZoneHandler(
  IRepositoryBase<Zone> zoneRepo,
  IReadRepositoryBase<Table> tableRepo)
  : ICommandHandler<UpdateZoneCommand, Result<ZoneDto>>
{
  public async ValueTask<Result<ZoneDto>> Handle(UpdateZoneCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return Result.Invalid(new ValidationError("Name", "Zone name is required."));

    var zone = await zoneRepo.FirstOrDefaultAsync(new ZoneByIdSpec(request.ZoneId), ct);
    if (zone is null)
      return Result.NotFound($"Zone {request.ZoneId} not found.");

    var duplicate = await zoneRepo.FirstOrDefaultAsync(new ZoneByNameSpec(request.Name), ct);
    if (duplicate is not null && duplicate.Id != request.ZoneId)
      return Result.Invalid(new ValidationError("Name", $"Zone '{request.Name}' already exists."));

    zone.UpdateName(request.Name);
    zone.SetDisplayOrder(request.DisplayOrder);
    await zoneRepo.UpdateAsync(zone, ct);

    var tables = await tableRepo.ListAsync(new AllTablesSpec(), ct);
    var tableCount = tables.Count(t => t.ZoneId == zone.Id);

    return Result.Success(new ZoneDto(zone.Id, zone.Name, zone.DisplayOrder, zone.IsActive, tableCount));
  }
}

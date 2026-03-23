using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;

namespace Api.UseCases.Zones.Delete;

public class DeleteZoneHandler(
  IRepositoryBase<Zone> zoneRepo,
  IReadRepositoryBase<Table> tableRepo)
  : ICommandHandler<DeleteZoneCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteZoneCommand request, CancellationToken ct)
  {
    var zone = await zoneRepo.FirstOrDefaultAsync(new ZoneByIdSpec(request.ZoneId), ct);
    if (zone is null)
      return Result.NotFound($"Zone {request.ZoneId} not found.");

    var tableCount = await tableRepo.CountAsync(new TablesByZoneIdSpec(request.ZoneId), ct);
    if (tableCount > 0)
      return Result.Conflict($"Cannot delete zone '{zone.Name}' — it has {tableCount} table(s) assigned. Reassign tables first.");

    zone.Delete(request.DeletedBy);
    await zoneRepo.UpdateAsync(zone, ct);

    return Result.Success();
  }
}

using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;

namespace Api.UseCases.Zones.Activate;

public class ActivateZoneHandler(IRepositoryBase<Zone> repository)
  : ICommandHandler<ActivateZoneCommand, Result>
{
  public async ValueTask<Result> Handle(ActivateZoneCommand request, CancellationToken ct)
  {
    var zone = await repository.FirstOrDefaultAsync(new ZoneByIdSpec(request.ZoneId), ct);
    if (zone is null)
      return Result.NotFound($"Zone {request.ZoneId} not found.");

    zone.Activate();
    await repository.UpdateAsync(zone, ct);

    return Result.Success();
  }
}

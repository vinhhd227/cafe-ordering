using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;
using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.Create;

public class CreateZoneHandler(IRepositoryBase<Zone> repository)
  : ICommandHandler<CreateZoneCommand, Result<ZoneDto>>
{
  public async ValueTask<Result<ZoneDto>> Handle(CreateZoneCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return Result.Invalid(new ValidationError("Name", "Zone name is required."));

    var existing = await repository.FirstOrDefaultAsync(new ZoneByNameSpec(request.Name), ct);
    if (existing is not null)
      return Result.Invalid(new ValidationError("Name", $"Zone '{request.Name}' already exists."));

    var zone = Zone.Create(request.Name, request.DisplayOrder);
    await repository.AddAsync(zone, ct);

    return Result.Success(new ZoneDto(zone.Id, zone.Name, zone.DisplayOrder, zone.IsActive, 0));
  }
}

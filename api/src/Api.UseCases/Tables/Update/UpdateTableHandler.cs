using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Aggregates.ZoneAggregate;
using Api.Core.Aggregates.ZoneAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Update;

public class UpdateTableHandler(
  IRepositoryBase<Table> repository,
  IReadRepositoryBase<Zone> zoneRepo)
  : ICommandHandler<UpdateTableCommand, Result<TableDto>>
{
  public async ValueTask<Result<TableDto>> Handle(UpdateTableCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Code))
      return Result.Invalid(new ValidationError("Code", "Table code is required."));

    var table = await repository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);
    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (request.ZoneId.HasValue)
    {
      var zone = await zoneRepo.FirstOrDefaultAsync(new ZoneByIdSpec(request.ZoneId.Value), ct);
      if (zone is null)
        return Result.Invalid(new ValidationError("ZoneId", $"Zone {request.ZoneId} not found."));
    }

    var duplicate = await repository.FirstOrDefaultAsync(new TableByCodeSpec(request.Code), ct);
    if (duplicate is not null && duplicate.Id != request.TableId)
      return Result.Invalid(new ValidationError("Code", $"Table code '{request.Code}' already exists."));

    table.UpdateCode(request.Code);
    table.AssignZone(request.ZoneId);
    await repository.UpdateAsync(table, ct);

    return Result.Success(new TableDto(table.Id, table.Code, table.IsActive, table.Status.ToString(), table.ActiveSessionId, table.ZoneId, table.Zone?.Name, table.QrToken));
  }
}

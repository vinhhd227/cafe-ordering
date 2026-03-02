using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Update;

public class UpdateTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<UpdateTableCommand, Result<TableDto>>
{
  public async ValueTask<Result<TableDto>> Handle(UpdateTableCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Code))
      return Result.Invalid(new ValidationError("Code", "Table code is required."));

    var table = await repository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);
    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    var duplicate = await repository.FirstOrDefaultAsync(new TableByCodeSpec(request.Code), ct);
    if (duplicate is not null && duplicate.Id != request.TableId)
      return Result.Invalid(new ValidationError("Code", $"Table code '{request.Code}' already exists."));

    table.UpdateCode(request.Code);
    await repository.UpdateAsync(table, ct);

    return Result.Success(new TableDto(table.Id, table.Code, table.IsActive, table.Status.ToString(), table.ActiveSessionId));
  }
}

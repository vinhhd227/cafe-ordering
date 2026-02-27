using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Update;

public class UpdateTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<UpdateTableCommand, Result<TableDto>>
{
  public async ValueTask<Result<TableDto>> Handle(UpdateTableCommand request, CancellationToken ct)
  {
    if (request.Number <= 0)
      return Result.Invalid(new ValidationError("Number", "Table number must be greater than zero."));

    if (string.IsNullOrWhiteSpace(request.Code))
      return Result.Invalid(new ValidationError("Code", "Table code is required."));

    var spec = new TableByIdSpec(request.TableId);
    var table = await repository.FirstOrDefaultAsync(spec, ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    table.UpdateNumber(request.Number);
    table.UpdateCode(request.Code);
    await repository.UpdateAsync(table, ct);

    return Result.Success(new TableDto(table.Id, table.Number, table.Code, table.IsActive, table.Status.ToString(), table.ActiveSessionId));
  }
}

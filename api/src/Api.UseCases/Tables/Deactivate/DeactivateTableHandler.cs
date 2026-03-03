using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

namespace Api.UseCases.Tables.Deactivate;

public class DeactivateTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<DeactivateTableCommand, Result>
{
  public async ValueTask<Result> Handle(DeactivateTableCommand request, CancellationToken ct)
  {
    var table = await repository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (table.Status == TableStatus.Occupied)
      return Result.Conflict("Cannot deactivate a table with an active session.");

    table.Deactivate();
    await repository.UpdateAsync(table, ct);
    return Result.Success();
  }
}

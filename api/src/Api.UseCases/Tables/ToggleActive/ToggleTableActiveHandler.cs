using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

namespace Api.UseCases.Tables.ToggleActive;

public class ToggleTableActiveHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<ToggleTableActiveCommand, Result>
{
  public async ValueTask<Result> Handle(ToggleTableActiveCommand request, CancellationToken ct)
  {
    var spec = new TableByIdSpec(request.TableId);
    var table = await repository.FirstOrDefaultAsync(spec, ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (table.Status == TableStatus.Occupied)
      return Result.Conflict("Cannot deactivate a table with an active session.");

    if (table.IsActive)
      table.Deactivate();
    else
      table.Activate();

    await repository.UpdateAsync(table, ct);
    return Result.Success();
  }
}

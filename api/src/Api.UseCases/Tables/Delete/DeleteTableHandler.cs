using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

namespace Api.UseCases.Tables.Delete;

public class DeleteTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<DeleteTableCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteTableCommand request, CancellationToken ct)
  {
    var spec = new TableByIdSpec(request.TableId);
    var table = await repository.FirstOrDefaultAsync(spec, ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (table.Status == TableStatus.Occupied)
      return Result.Conflict("Cannot delete a table with an active session. Close the session first.");

    table.Delete(request.DeletedBy);
    await repository.UpdateAsync(table, ct);

    return Result.Success();
  }
}

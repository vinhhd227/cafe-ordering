using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

namespace Api.UseCases.Tables.MarkAvailable;

public class MarkTableAvailableHandler(IRepositoryBase<Table> tableRepository)
  : ICommandHandler<MarkTableAvailableCommand, Result>
{
  public async ValueTask<Result> Handle(MarkTableAvailableCommand request, CancellationToken ct)
  {
    var tableSpec = new TableByIdSpec(request.TableId);
    var table = await tableRepository.FirstOrDefaultAsync(tableSpec, ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (table.Status == TableStatus.Occupied)
      return Result.Conflict($"Table {request.TableId} has an active session. Close the session first.");

    table.MarkAvailable();
    await tableRepository.UpdateAsync(table, ct);

    return Result.Success();
  }
}

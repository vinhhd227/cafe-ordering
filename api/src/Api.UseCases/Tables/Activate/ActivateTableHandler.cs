using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

namespace Api.UseCases.Tables.Activate;

public class ActivateTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<ActivateTableCommand, Result>
{
  public async ValueTask<Result> Handle(ActivateTableCommand request, CancellationToken ct)
  {
    var table = await repository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    table.Activate();
    await repository.UpdateAsync(table, ct);
    return Result.Success();
  }
}

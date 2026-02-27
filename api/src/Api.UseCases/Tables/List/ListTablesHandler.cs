using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.List;

public class ListTablesHandler(IReadRepositoryBase<Table> repository)
  : IQueryHandler<ListTablesQuery, Result<List<TableDto>>>
{
  public async ValueTask<Result<List<TableDto>>> Handle(ListTablesQuery request, CancellationToken ct)
  {
    var spec = new AllTablesSpec();
    var tables = await repository.ListAsync(spec, ct);

    var dtos = tables.Select(t => new TableDto(
      t.Id,
      t.Number,
      t.Code,
      t.IsActive,
      t.Status.ToString(),
      t.ActiveSessionId
    )).ToList();

    return Result.Success(dtos);
  }
}

using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.ListPublic;

public class ListPublicTablesHandler(IReadRepositoryBase<Table> repository)
  : IQueryHandler<ListPublicTablesQuery, Result<List<PublicTableDto>>>
{
  public async ValueTask<Result<List<PublicTableDto>>> Handle(ListPublicTablesQuery request, CancellationToken ct)
  {
    var spec = new ActiveTablesSpec();
    var tables = await repository.ListAsync(spec, ct);

    var dtos = tables
      .Select(t => new PublicTableDto(t.Id, t.Code, t.Status.ToString()))
      .ToList();

    return Result.Success(dtos);
  }
}

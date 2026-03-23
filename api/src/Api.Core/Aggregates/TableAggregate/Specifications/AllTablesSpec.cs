using Api.Core.Aggregates.ZoneAggregate;

namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class AllTablesSpec : Specification<Table>
{
  public AllTablesSpec()
  {
    Query.Where(t => !t.IsDeleted);
    Query.Include(t => t.Zone);
    Query.OrderBy(t => t.Code);
  }
}

namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class AllTablesSpec : Specification<Table>
{
  public AllTablesSpec()
  {
    Query.Where(t => !t.IsDeleted);
    Query.OrderBy(t => t.Number);
  }
}

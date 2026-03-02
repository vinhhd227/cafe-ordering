namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class TablesByIdsSpec : Specification<Table>
{
  public TablesByIdsSpec(IEnumerable<int> ids)
  {
    Query.Where(t => ids.Contains(t.Id) && !t.IsDeleted);
  }
}

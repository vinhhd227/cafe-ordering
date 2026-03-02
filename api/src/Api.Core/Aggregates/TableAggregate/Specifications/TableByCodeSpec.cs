namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class TableByCodeSpec : Specification<Table>
{
  public TableByCodeSpec(string code)
  {
    Query.Where(t => t.Code == code && !t.IsDeleted);
  }
}

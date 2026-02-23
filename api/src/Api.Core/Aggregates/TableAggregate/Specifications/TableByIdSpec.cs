namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class TableByIdSpec : Specification<Table>
{
  public TableByIdSpec(int tableId)
  {
    Query.Where(t => t.Id == tableId && !t.IsDeleted);
  }
}

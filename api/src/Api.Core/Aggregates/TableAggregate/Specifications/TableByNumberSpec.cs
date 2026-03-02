namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class TableByNumberSpec : Specification<Table>
{
  public TableByNumberSpec(int number)
  {
    Query.Where(t => t.Number == number && !t.IsDeleted);
  }
}

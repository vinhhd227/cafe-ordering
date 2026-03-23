namespace Api.Core.Aggregates.ZoneAggregate.Specifications;

public class ZoneByNameSpec : SingleResultSpecification<Zone>
{
  public ZoneByNameSpec(string name)
  {
    Query.Where(z => z.Name == name && !z.IsDeleted);
  }
}

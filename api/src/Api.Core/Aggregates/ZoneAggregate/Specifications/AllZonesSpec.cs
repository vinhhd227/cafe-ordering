namespace Api.Core.Aggregates.ZoneAggregate.Specifications;

public class AllZonesSpec : Specification<Zone>
{
  public AllZonesSpec()
  {
    Query.Where(z => !z.IsDeleted);
    Query.OrderBy(z => z.DisplayOrder).ThenBy(z => z.Name);
  }
}

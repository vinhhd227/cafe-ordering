namespace Api.Core.Aggregates.ZoneAggregate.Specifications;

public class ActiveZonesSpec : Specification<Zone>
{
  public ActiveZonesSpec()
  {
    Query.Where(z => z.IsActive && !z.IsDeleted);
    Query.OrderBy(z => z.DisplayOrder).ThenBy(z => z.Name);
  }
}

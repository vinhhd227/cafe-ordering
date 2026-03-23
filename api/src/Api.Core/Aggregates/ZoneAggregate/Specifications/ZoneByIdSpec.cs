namespace Api.Core.Aggregates.ZoneAggregate.Specifications;

public class ZoneByIdSpec : Specification<Zone>
{
  public ZoneByIdSpec(int zoneId)
  {
    Query.Where(z => z.Id == zoneId && !z.IsDeleted);
  }
}

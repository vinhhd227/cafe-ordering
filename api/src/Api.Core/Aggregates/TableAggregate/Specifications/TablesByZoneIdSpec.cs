namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class TablesByZoneIdSpec : Specification<Table>
{
  public TablesByZoneIdSpec(int zoneId)
  {
    Query.Where(t => t.ZoneId == zoneId && !t.IsDeleted);
  }
}

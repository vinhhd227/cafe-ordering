namespace Api.Core.Aggregates.GuestSessionAggregate.Specifications;

public class SessionsByTableIdsSpec : Specification<GuestSession>
{
  public SessionsByTableIdsSpec(IEnumerable<int> tableIds)
  {
    Query.Where(s => s.TableId.HasValue && tableIds.Contains(s.TableId.Value));
  }
}

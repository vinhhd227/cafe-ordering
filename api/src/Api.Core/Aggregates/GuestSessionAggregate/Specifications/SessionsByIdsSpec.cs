namespace Api.Core.Aggregates.GuestSessionAggregate.Specifications;

public class SessionsByIdsSpec : Specification<GuestSession>
{
  public SessionsByIdsSpec(IEnumerable<Guid> ids)
  {
    Query.Where(s => ids.Contains(s.Id));
  }
}

namespace Api.Core.Aggregates.GuestSessionAggregate.Specifications;

public class SessionByIdSpec : Specification<GuestSession>
{
  public SessionByIdSpec(Guid sessionId)
  {
    Query.Where(s => s.Id == sessionId);
  }
}

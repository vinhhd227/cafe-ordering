namespace Api.Core.Aggregates.OrderAggregate.Specifications;

public class LatestOrderBySessionIdSpec : SingleResultSpecification<Order>
{
  public LatestOrderBySessionIdSpec(Guid sessionId) =>
    Query.Where(o => o.SessionId == sessionId)
         .OrderByDescending(o => o.OrderDate);
}

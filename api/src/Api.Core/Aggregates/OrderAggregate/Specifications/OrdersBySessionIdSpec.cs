using Api.Core.Aggregates.OrderAggregate;

namespace Api.Core.Aggregates.OrderAggregate.Specifications;

public class OrdersBySessionIdSpec : Specification<Order>
{
  public OrdersBySessionIdSpec(Guid sessionId, DateTime? since = null)
  {
    Query.Where(o => o.SessionId == sessionId && (since == null || o.OrderDate >= since));
    Query.Include(o => o.Items);
  }
}

using Api.Core.Aggregates.OrderAggregate;

namespace Api.Core.Aggregates.OrderAggregate.Specifications;

public class OrdersBySessionIdSpec : Specification<Order>
{
  public OrdersBySessionIdSpec(Guid sessionId)
  {
    Query.Where(o => o.SessionId == sessionId);
  }
}

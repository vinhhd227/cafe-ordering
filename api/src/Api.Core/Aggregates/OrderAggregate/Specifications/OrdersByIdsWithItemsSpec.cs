namespace Api.Core.Aggregates.OrderAggregate.Specifications;

public class OrdersByIdsWithItemsSpec : Specification<Order>
{
  public OrdersByIdsWithItemsSpec(IEnumerable<int> ids)
  {
    Query
      .Where(o => ids.Contains(o.Id))
      .Include(o => o.Items);
  }
}

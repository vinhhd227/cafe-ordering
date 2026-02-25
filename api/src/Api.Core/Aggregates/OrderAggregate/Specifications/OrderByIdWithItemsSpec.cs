namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Lấy một Order theo Id, include Items
/// </summary>
public class OrderByIdWithItemsSpec : SingleResultSpecification<Order>
{
  public OrderByIdWithItemsSpec(int orderId)
  {
    Query
      .Where(o => o.Id == orderId)
      .Include(o => o.Items);
  }
}

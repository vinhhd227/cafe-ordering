namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Load một Order theo Id, include Items và Promotions (cần thiết cho apply/remove promotion).
/// </summary>
public class OrderByIdWithItemsAndPromotionsSpec : SingleResultSpecification<Order>
{
  public OrderByIdWithItemsAndPromotionsSpec(int orderId)
    => Query
      .Where(o => o.Id == orderId)
      .Include(o => o.Items)
      .Include(o => o.Promotions);
}

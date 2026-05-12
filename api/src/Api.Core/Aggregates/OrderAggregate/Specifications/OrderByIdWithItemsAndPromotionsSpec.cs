namespace Api.Core.Aggregates.OrderAggregate.Specifications;

public class OrderByIdWithItemsAndPromotionsSpec : SingleResultSpecification<Order>
{
  public OrderByIdWithItemsAndPromotionsSpec(int orderId)
    => Query
      .Where(o => o.Id == orderId)
      .Include(o => o.Items)
        .ThenInclude(i => i.SelectedOptions)
      .Include(o => o.Promotions);
}

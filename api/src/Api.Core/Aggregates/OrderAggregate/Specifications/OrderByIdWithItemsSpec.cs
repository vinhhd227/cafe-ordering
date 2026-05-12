namespace Api.Core.Aggregates.OrderAggregate.Specifications;

public class OrderByIdWithItemsSpec : SingleResultSpecification<Order>
{
  public OrderByIdWithItemsSpec(int orderId)
  {
    Query
      .Where(o => o.Id == orderId)
      .Include(o => o.Items)
        .ThenInclude(i => i.SelectedOptions);
  }
}

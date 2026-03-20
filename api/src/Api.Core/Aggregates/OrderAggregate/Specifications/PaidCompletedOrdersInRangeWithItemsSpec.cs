namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Loads COMPLETED + PAID orders with their Items within [from, to).
///   Used for monthly comparison report aggregation.
/// </summary>
public class PaidCompletedOrdersInRangeWithItemsSpec : Specification<Order>
{
  public PaidCompletedOrdersInRangeWithItemsSpec(DateTime from, DateTime to)
  {
    var completed = OrderStatus.Completed;
    var paid      = PaymentStatus.Paid;

    Query
      .Where(o => o.Status == completed
               && o.PaymentStatus == paid
               && o.OrderDate >= from
               && o.OrderDate < to)
      .Include(o => o.Items);
  }
}

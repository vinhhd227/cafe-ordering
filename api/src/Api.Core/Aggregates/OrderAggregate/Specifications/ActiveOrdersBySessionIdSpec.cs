namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Trả về bất kỳ order nào chưa "xong hoàn toàn" trong session (24h gần đây).
///   "Xong hoàn toàn" = CANCELLED hoặc (COMPLETED + PAID).
///   Dùng để xác định session còn phục vụ hay không — chỉ close khi mọi order đã done.
/// </summary>
public class ActiveOrdersBySessionIdSpec : Specification<Order>
{
  public ActiveOrdersBySessionIdSpec(Guid sessionId)
  {
    var pending    = OrderStatus.Pending;
    var processing = OrderStatus.Processing;
    var completed  = OrderStatus.Completed;
    var paid       = PaymentStatus.Paid;
    var cutoff     = DateTime.UtcNow.AddHours(-24);

    Query
      .Where(o => o.SessionId == sessionId
               && o.OrderDate >= cutoff
               && (o.Status == pending
                || o.Status == processing
                || (o.Status == completed && o.PaymentStatus != paid)))
      .Take(1);
  }
}

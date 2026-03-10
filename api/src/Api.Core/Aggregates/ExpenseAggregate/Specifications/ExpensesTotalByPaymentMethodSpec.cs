using Api.Core.Aggregates.OrderAggregate;

namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Projection spec: lấy TotalAmount của Expense theo phương thức thanh toán (Cash/BankTransfer)
///   trong khoảng ngày mua. Dùng để tổng hợp chi phí theo hình thức thanh toán (P&L).
/// </summary>
public class ExpensesTotalByPaymentMethodSpec : Specification<Expense, decimal>
{
  public ExpensesTotalByPaymentMethodSpec(
    PaymentMethod paymentMethod,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    Query
      .Where(e => !e.IsDeleted && e.PaymentMethod == paymentMethod)
      .Select(e => e.TotalAmount);

    if (dateFrom.HasValue)
      Query.Where(e => e.PurchaseDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(e => e.PurchaseDate < dateTo.Value.AddDays(1));
  }
}

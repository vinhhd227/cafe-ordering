using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.ExpenseAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.GetSummary;

public class GetExpenseSummaryHandler(
  IReadRepositoryBase<Expense> expenseRepository,
  IReadRepositoryBase<Order> orderRepository)
  : IQueryHandler<GetExpenseSummaryQuery, Result<ExpenseSummaryDto>>
{
  public async ValueTask<Result<ExpenseSummaryDto>> Handle(
    GetExpenseSummaryQuery request, CancellationToken ct)
  {
    // ToUniversalTime() xử lý đúng khi server chạy ở timezone Vietnam:
    // ASP.NET model binding parse "2026-03-09T17:00:00Z" → Local (March 10 00:00 VN),
    // ToUniversalTime() convert lại về March 9 17:00 UTC đúng.
    var dateFrom = request.DateFrom.HasValue
      ? request.DateFrom.Value.ToUniversalTime()
      : (DateTime?)null;
    var dateTo = request.DateTo.HasValue
      ? request.DateTo.Value.ToUniversalTime()
      : (DateTime?)null;

    var categoryFilter = string.IsNullOrWhiteSpace(request.Category)
      ? (ExpenseCategory?)null
      : ExpenseCategory.FromName(request.Category, true);
    var paymentFilter = string.IsNullOrWhiteSpace(request.PaymentMethod)
      ? (PaymentMethod?)null
      : PaymentMethod.FromName(request.PaymentMethod, true);

    // ── Revenue từ Orders (PAID) — luôn hiển thị toàn bộ ─────────────
    var cashTotals = await orderRepository.ListAsync(new PaidOrdersTotalSpec(PaymentMethod.Cash,        dateFrom: dateFrom, dateTo: dateTo), ct);
    var bankTotals = await orderRepository.ListAsync(new PaidOrdersTotalSpec(PaymentMethod.BankTransfer, dateFrom: dateFrom, dateTo: dateTo), ct);

    var cashTotal = cashTotals.Sum();
    var bankTotal = bankTotals.Sum();
    var revenue   = new RevenueSummaryDto(Cash: cashTotal, Bank: bankTotal, Total: cashTotal + bankTotal);

    // ── Expenses theo category ─────────────────────────────────────────
    // Nếu có categoryFilter: chỉ query category đó, các category khác = 0
    async Task<decimal> SumCategory(ExpenseCategory cat) =>
      categoryFilter is null || categoryFilter == cat
        ? (await expenseRepository.ListAsync(new ExpensesTotalByCategorySpec(cat, dateFrom, dateTo, paymentFilter), ct)).Sum()
        : 0m;

    var ingredient   = await SumCategory(ExpenseCategory.Ingredient);
    var supply       = await SumCategory(ExpenseCategory.Supply);
    var equipment    = await SumCategory(ExpenseCategory.Equipment);
    var other        = await SumCategory(ExpenseCategory.Other);
    var expenseTotal = ingredient + supply + equipment + other;

    // ── Expenses theo payment method ──────────────────────────────────
    // Nếu có paymentFilter: slot tương ứng = expenseTotal, slot còn lại = 0
    // Nếu không: query từng method (có thể kết hợp categoryFilter)
    decimal expCash, expBank;
    if (paymentFilter == PaymentMethod.Cash)
    {
      expCash = expenseTotal;
      expBank = 0m;
    }
    else if (paymentFilter == PaymentMethod.BankTransfer)
    {
      expCash = 0m;
      expBank = expenseTotal;
    }
    else
    {
      expCash = (await expenseRepository.ListAsync(new ExpensesTotalByPaymentMethodSpec(PaymentMethod.Cash,        dateFrom, dateTo, categoryFilter), ct)).Sum();
      expBank = (await expenseRepository.ListAsync(new ExpensesTotalByPaymentMethodSpec(PaymentMethod.BankTransfer, dateFrom, dateTo, categoryFilter), ct)).Sum();
    }

    var expenses = new ExpenseByCategoryDto(
      ingredient, supply, equipment, other, expenseTotal,
      Cash: expCash, Bank: expBank);

    var profit = revenue.Total - expenseTotal;

    return Result.Success(new ExpenseSummaryDto(revenue, expenses, profit));
  }
}

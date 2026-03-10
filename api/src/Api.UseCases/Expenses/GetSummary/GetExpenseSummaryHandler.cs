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

    // ── Revenue từ Orders (PAID) ──────────────────────────────────
    var cashSpec = new PaidOrdersTotalSpec(PaymentMethod.Cash,
      dateFrom: dateFrom, dateTo: dateTo);
    var bankSpec = new PaidOrdersTotalSpec(PaymentMethod.BankTransfer,
      dateFrom: dateFrom, dateTo: dateTo);

    var cashTotals = await orderRepository.ListAsync(cashSpec, ct);
    var bankTotals = await orderRepository.ListAsync(bankSpec, ct);

    var cashTotal = cashTotals.Sum();
    var bankTotal = bankTotals.Sum();

    var revenue = new RevenueSummaryDto(
      Cash:  cashTotal,
      Bank:  bankTotal,
      Total: cashTotal + bankTotal);

    // ── Expenses theo category ─────────────────────────────────────
    var ingredientAmounts = await expenseRepository.ListAsync(
      new ExpensesTotalByCategorySpec(ExpenseCategory.Ingredient, dateFrom, dateTo), ct);
    var supplyAmounts = await expenseRepository.ListAsync(
      new ExpensesTotalByCategorySpec(ExpenseCategory.Supply, dateFrom, dateTo), ct);
    var equipmentAmounts = await expenseRepository.ListAsync(
      new ExpensesTotalByCategorySpec(ExpenseCategory.Equipment, dateFrom, dateTo), ct);
    var otherAmounts = await expenseRepository.ListAsync(
      new ExpensesTotalByCategorySpec(ExpenseCategory.Other, dateFrom, dateTo), ct);

    var ingredient   = ingredientAmounts.Sum();
    var supply       = supplyAmounts.Sum();
    var equipment    = equipmentAmounts.Sum();
    var other        = otherAmounts.Sum();
    var expenseTotal = ingredient + supply + equipment + other;

    // ── Expenses theo payment method ───────────────────────────────
    var expCashAmounts = await expenseRepository.ListAsync(
      new ExpensesTotalByPaymentMethodSpec(PaymentMethod.Cash, dateFrom, dateTo), ct);
    var expBankAmounts = await expenseRepository.ListAsync(
      new ExpensesTotalByPaymentMethodSpec(PaymentMethod.BankTransfer, dateFrom, dateTo), ct);

    var expCash = expCashAmounts.Sum();
    var expBank = expBankAmounts.Sum();

    var expenses = new ExpenseByCategoryDto(
      ingredient, supply, equipment, other, expenseTotal,
      Cash: expCash, Bank: expBank);

    var profit = revenue.Total - expenseTotal;

    return Result.Success(new ExpenseSummaryDto(revenue, expenses, profit));
  }
}

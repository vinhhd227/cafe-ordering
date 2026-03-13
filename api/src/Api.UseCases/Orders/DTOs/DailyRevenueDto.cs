namespace Api.UseCases.Orders.DTOs;

public record DailyRevenueDto(
  DateOnly Date,
  decimal Revenue,
  decimal CashRevenue,
  decimal BankRevenue,
  int OrderCount,
  int CompletedCount,
  int ItemsSold
);

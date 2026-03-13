namespace Api.UseCases.Orders.DTOs;

public record OrdersSummaryDto(
  decimal CashRevenue,
  decimal BankRevenue,
  decimal TotalRevenue,
  decimal TotalTips,
  int TotalOrders,
  int CompletedOrders,
  int CancelledOrders,
  int PendingOrders,
  int ProcessingOrders,
  int TotalItemsSold,
  List<DailyRevenueDto> DailyRevenue
);

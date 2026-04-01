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
  int TotalGuestCount,
  List<DailyRevenueDto> DailyRevenue,
  List<TopProductDto> TopProducts,
  List<TopCategoryDto> TopCategories,
  List<HourlyRevenueDto> HourlyRevenue,
  decimal? PrevPeriodRevenue,
  decimal? Avg30DayRevenue
);

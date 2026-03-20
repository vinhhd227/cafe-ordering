namespace Api.UseCases.Orders.DTOs;

public record MonthlyComparisonDto(
  int Year,
  int Month,
  MonthSummaryDto Current,
  MonthSummaryDto Previous,
  List<ProductComparisonDto> TopProducts,
  List<ProductComparisonDto> WorstSellers,
  List<HourlyDataDto> PeakHours,
  List<WeekdayDataDto> WeekdayPattern,
  decimal TakeawayRatio
);

/// <param name="AvgOrderValue">TotalRevenue / TotalOrders (0 if no orders)</param>
public record MonthSummaryDto(
  decimal TotalRevenue,
  decimal CashRevenue,
  decimal BankRevenue,
  int TotalOrders,
  int TotalItemsSold,
  int TotalGuestCount,
  decimal AvgOrderValue
);

/// <param name="Rank">Rank trong tháng hiện tại (1 = cao nhất)</param>
/// <param name="PrevRank">null nếu không xuất hiện trong tháng trước</param>
/// <param name="RankChange">Dương = tăng hạng, âm = giảm hạng, 0 = giữ nguyên</param>
public record ProductComparisonDto(
  string Name,
  int Qty,
  decimal Revenue,
  int Rank,
  int? PrevRank,
  int RankChange
);

public record HourlyDataDto(int Hour, decimal Revenue, int OrderCount);

/// <param name="DayOfWeek">ISO weekday: 1=Mon, 2=Tue, ..., 7=Sun</param>
public record WeekdayDataDto(int DayOfWeek, decimal Revenue, int OrderCount);

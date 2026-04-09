namespace Api.UseCases.Orders.DTOs;

public record DailyReportRevenueDto(
  decimal Total,
  decimal Cash,
  decimal Transfer,
  decimal Yesterday,
  decimal Avg7Days,
  decimal Avg30Days
);

public record DailyReportOrdersDto(
  int Total,
  int Completed,
  int Cancelled,
  int Pending
);

public record DailyReportAveragesDto(
  decimal RevenuePerOrder,
  double ItemsPerOrder,
  decimal RevenuePerOrderMonthAvg,
  double ItemsPerOrderMonthAvg
);

public record DailyReportPeakHourDto(
  string Hour,
  decimal Revenue,
  int Percentage
);

public record DailyReportTopProductDto(
  string ProductId,
  string Name,
  int Quantity,
  decimal Revenue
);

public record DailyReportTopCategoryDto(
  string Name,
  int Percentage,
  decimal Revenue
);

/// <summary>Hourly revenue entry — Hour in "HH:00" format.</summary>
public record DailyReportHourlyDto(string Hour, decimal Revenue);

public record DailyReportDto(
  DateOnly Date,
  DailyReportRevenueDto Revenue,
  DailyReportOrdersDto Orders,
  DailyReportAveragesDto Averages,
  DailyReportPeakHourDto? PeakHour,
  List<DailyReportTopProductDto> TopProducts,
  List<DailyReportTopCategoryDto> TopCategories,
  List<DailyReportHourlyDto> HourlyRevenue,
  List<DailyReportHourlyDto> HourlyAvg7Days
);

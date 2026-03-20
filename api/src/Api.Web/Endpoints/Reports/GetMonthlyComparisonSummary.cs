using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Reports;

public class GetMonthlyComparisonSummary : Summary<GetMonthlyComparison>
{
  public GetMonthlyComparisonSummary()
  {
    Summary = "Get monthly comparison report";
    Description =
      "Returns aggregated statistics for the given month compared to the previous month. " +
      "Includes KPI deltas, top products with rank changes, worst sellers, peak hours by hour-of-day, " +
      "weekday pattern, and takeaway ratio. Only COMPLETED + PAID orders are counted. Requires report.read permission.";

    ExampleRequest = new GetMonthlyComparisonRequest { Year = 2026, Month = 3 };

    Response<MonthlyComparisonDto>(200, "Monthly comparison data.", example: new MonthlyComparisonDto(
      Year: 2026,
      Month: 3,
      Current: new MonthSummaryDto(
        TotalRevenue: 50_000_000,
        CashRevenue: 30_000_000,
        BankRevenue: 20_000_000,
        TotalOrders: 320,
        TotalItemsSold: 850,
        TotalGuestCount: 640,
        AvgOrderValue: 156_250),
      Previous: new MonthSummaryDto(
        TotalRevenue: 42_000_000,
        CashRevenue: 26_000_000,
        BankRevenue: 16_000_000,
        TotalOrders: 285,
        TotalItemsSold: 730,
        TotalGuestCount: 570,
        AvgOrderValue: 147_368),
      TopProducts:
      [
        new ProductComparisonDto("Bạc xỉu", 180, 18_000_000, 1, 2, 1),
        new ProductComparisonDto("Cà phê sữa đá", 155, 15_500_000, 2, 1, -1),
      ],
      WorstSellers:
      [
        new ProductComparisonDto("Trà hoa cúc", 5, 125_000, 15, null, 0),
      ],
      PeakHours:
      [
        new HourlyDataDto(7,  2_000_000, 12),
        new HourlyDataDto(8,  5_500_000, 35),
        new HourlyDataDto(9,  4_200_000, 28),
      ],
      WeekdayPattern:
      [
        new WeekdayDataDto(1, 8_000_000, 45),
        new WeekdayDataDto(6, 9_500_000, 58),
        new WeekdayDataDto(7, 10_000_000, 62),
      ],
      TakeawayRatio: 32.5m
    ));

    Response(400, "Invalid year or month value.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions — report.read required.");
  }
}

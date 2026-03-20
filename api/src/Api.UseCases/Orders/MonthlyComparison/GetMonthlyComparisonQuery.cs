using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.MonthlyComparison;

public record GetMonthlyComparisonQuery(int Year, int Month)
  : IQuery<Result<MonthlyComparisonDto>>;

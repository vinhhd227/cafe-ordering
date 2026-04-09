using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.DailyReport;

public record GetDailyReportQuery(DateOnly Date) : IQuery<Result<DailyReportDto>>;

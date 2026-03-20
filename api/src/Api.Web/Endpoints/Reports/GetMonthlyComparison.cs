using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.MonthlyComparison;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Reports;

public sealed class GetMonthlyComparisonRequest
{
  public int Year  { get; set; }
  public int Month { get; set; }
}

public class GetMonthlyComparison(IMediator mediator)
  : Endpoint<GetMonthlyComparisonRequest, MonthlyComparisonDto>
{
  public override void Configure()
  {
    Get("/api/admin/reports/monthly-comparison");
    Policies("report.read");
    DontAutoTag();
    Description(b => b.WithTags("Reports"));
  }

  public override async Task HandleAsync(GetMonthlyComparisonRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetMonthlyComparisonQuery(req.Year, req.Month), ct);
    await this.SendResultAsync(result, ct);
  }
}

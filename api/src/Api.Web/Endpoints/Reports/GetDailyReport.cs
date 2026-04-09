using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.DailyReport;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Reports;

public sealed class GetDailyReportRequest
{
  public DateOnly Date { get; set; }
}

public class GetDailyReport(IMediator mediator) : Endpoint<GetDailyReportRequest, DailyReportDto>
{
  public override void Configure()
  {
    Get("/api/admin/reports/daily");
    Policies("report.read");
    DontAutoTag();
    Description(b => b.WithTags("Reports"));
  }

  public override async Task HandleAsync(GetDailyReportRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetDailyReportQuery(req.Date), ct);
    await this.SendResultAsync(result, ct);
  }
}

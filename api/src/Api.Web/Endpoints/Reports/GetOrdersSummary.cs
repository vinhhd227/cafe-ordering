using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.Summary;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Reports;

public sealed class GetOrdersSummaryRequest
{
  public DateTime? DateFrom { get; set; }
  public DateTime? DateTo { get; set; }
}

public class GetOrdersSummary(IMediator mediator) : Endpoint<GetOrdersSummaryRequest, OrdersSummaryDto>
{
  public override void Configure()
  {
    Get("/api/admin/reports/orders-summary");
    Policies("report.read");
    DontAutoTag();
    Description(b => b.WithTags("Reports"));
  }

  public override async Task HandleAsync(GetOrdersSummaryRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetOrdersSummaryQuery(req.DateFrom, req.DateTo), ct);
    await this.SendResultAsync(result, ct);
  }
}

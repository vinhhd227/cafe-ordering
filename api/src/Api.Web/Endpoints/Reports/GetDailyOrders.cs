using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.DailyOrders;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Reports;

public sealed class GetDailyOrdersRequest
{
  public DateOnly Date { get; set; }
}

public class GetDailyOrders(IMediator mediator) : Endpoint<GetDailyOrdersRequest, DailyOrdersResponseDto>
{
  public override void Configure()
  {
    Get("/api/admin/reports/daily/orders");
    Policies("report.read");
    DontAutoTag();
    Description(b => b.WithTags("Reports"));
  }

  public override async Task HandleAsync(GetDailyOrdersRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetDailyOrdersQuery(req.Date), ct);
    await this.SendResultAsync(result, ct);
  }
}

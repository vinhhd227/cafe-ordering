using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class ListOrdersRequest
{
  [QueryParam] public string? Status { get; set; }
  [QueryParam] public DateTime? DateFrom { get; set; }
  [QueryParam] public DateTime? DateTo { get; set; }
  [QueryParam] public int Page { get; set; } = 1;
  [QueryParam] public int PageSize { get; set; } = 20;
}

public class ListOrders(IMediator mediator) : Endpoint<ListOrdersRequest, PagedOrdersDto>
{
  public override void Configure()
  {
    Get("/api/admin/orders");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(ListOrdersRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ListOrdersQuery(req.Status, req.DateFrom, req.DateTo, req.Page, req.PageSize), ct);
    await this.SendResultAsync(result, ct);
  }
}

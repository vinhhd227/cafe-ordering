using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class ListOrdersRequest
{
  [QueryParam] public string? Status { get; set; }
}

public class ListOrders(IMediator mediator) : Endpoint<ListOrdersRequest, List<OrderDto>>
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
    var result = await mediator.Send(new ListOrdersQuery(req.Status), ct);
    await this.SendResultAsync(result, ct);
  }
}

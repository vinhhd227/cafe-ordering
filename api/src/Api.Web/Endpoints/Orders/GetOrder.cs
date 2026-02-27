using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class GetOrderRequest
{
  public int Id { get; set; }
}

public class GetOrder(IMediator mediator) : Endpoint<GetOrderRequest, OrderDto>
{
  public override void Configure()
  {
    Get("/api/admin/orders/{id}");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(GetOrderRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetOrderQuery(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}

using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.UpdateItem;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class UpdateOrderItemPublicRequest
{
  public int  OrderId   { get; set; }
  public int  ProductId { get; set; }
  public int  Quantity  { get; set; }   // 0 = remove
  public Guid SessionId { get; set; }  // must match order's session (ownership check)
}

public class UpdateOrderItemPublic(IMediator mediator) : Endpoint<UpdateOrderItemPublicRequest, OrderDto>
{
  public override void Configure()
  {
    Put("/api/orders/{orderId}/items/{productId}");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(UpdateOrderItemPublicRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new UpdateOrderItemCommand(req.OrderId, req.ProductId, req.Quantity, req.SessionId), ct);
    await this.SendResultAsync(result, ct);
  }
}

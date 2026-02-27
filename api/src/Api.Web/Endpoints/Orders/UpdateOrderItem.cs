using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.UpdateItem;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class UpdateOrderItemRequest
{
  public int    OrderId   { get; set; }
  public int    ProductId { get; set; }
  public int    Quantity  { get; set; }  // 0 = remove
}

public class UpdateOrderItem(IMediator mediator) : Endpoint<UpdateOrderItemRequest, OrderDto>
{
  public override void Configure()
  {
    Put("/api/admin/orders/{orderId}/items/{productId}");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(UpdateOrderItemRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new UpdateOrderItemCommand(req.OrderId, req.ProductId, req.Quantity), ct);
    await this.SendResultAsync(result, ct);
  }
}

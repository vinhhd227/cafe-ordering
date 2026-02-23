using Api.UseCases.Orders.Create;
using Api.UseCases.Orders.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class CreateOrderRequest
{
  public Guid SessionId { get; set; }
  public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class CreateOrderItemRequest
{
  public int ProductId { get; set; }
  public string ProductName { get; set; } = string.Empty;
  public decimal UnitPrice { get; set; }
  public int Quantity { get; set; }
}

public class Create(IMediator mediator) : Endpoint<CreateOrderRequest, PlaceOrderResponseDto>
{
  public override void Configure()
  {
    Post("/api/orders");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(CreateOrderRequest req, CancellationToken ct)
  {
    var items = req.Items
      .Select(i => new PlaceOrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity))
      .ToList();

    var result = await mediator.Send(new PlaceOrderCommand(req.SessionId, items), ct);
    await this.SendResultAsync(result, ct);
  }
}

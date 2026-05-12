using Api.UseCases.Orders.Create;
using Api.UseCases.Orders.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class CreateOrderRequest
{
  public Guid SessionId { get; set; }
  public List<CreateOrderItemRequest> Items { get; set; } = [];
  public int? GuestCount { get; set; }
  public string? PromoCode { get; set; }
}

public sealed class CreateOrderItemOptionValueRequest
{
  public int OptionValueId { get; set; }
  public int Quantity { get; set; } = 1;
}

public sealed class CreateOrderItemRequest
{
  public int ProductId { get; set; }
  public string ProductName { get; set; } = string.Empty;
  public decimal UnitPrice { get; set; }
  public int Quantity { get; set; }
  public List<int>? SelectedOptionValueIds { get; set; }
  public List<CreateOrderItemOptionValueRequest>? SelectedOptionValues { get; set; }
  public bool IsTakeaway { get; set; }
  public bool IsFreeGift { get; set; }
  public string? Note { get; set; }
}

public class Create(IMediator mediator) : Endpoint<CreateOrderRequest, PlaceOrderResponseDto>
{
  public override void Configure()
  {
    Post("/api/orders");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
    Options(x => x.RequireRateLimiting("OrdersPerIp"));
  }

  public override async Task HandleAsync(CreateOrderRequest req, CancellationToken ct)
  {
    var items = req.Items
      .Select(i => new PlaceOrderItemDto(
        i.ProductId, i.ProductName, i.UnitPrice, i.Quantity,
        i.SelectedOptionValueIds,
        i.SelectedOptionValues?.Select(v => new PlaceOrderOptionValueInput(v.OptionValueId, v.Quantity)).ToList(),
        i.IsTakeaway, i.IsFreeGift, i.Note))
      .ToList();

    var bypassCooldown = User.HasClaim("permission", "admin.access");
    var result = await mediator.Send(
      new PlaceOrderCommand(req.SessionId, items, req.GuestCount, bypassCooldown, req.PromoCode), ct);
    await this.SendResultAsync(result, ct);
  }
}

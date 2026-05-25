using Api.UseCases.Orders.Create;
using Api.UseCases.Orders.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class CreateAdminOrderRequest
{
  /// <summary>DINE_IN | TAKEAWAY | DELIVERY</summary>
  public string OrderType { get; set; } = "DINE_IN";

  /// <summary>Required when OrderType = DINE_IN</summary>
  public Guid? SessionId { get; set; }

  public List<CreateOrderItemRequest> Items { get; set; } = [];
  public int? GuestCount { get; set; }
  public string? PromoCode { get; set; }

  // Takeaway / Delivery
  public string? CustomerName { get; set; }
  public string? CustomerPhone { get; set; }

  // Delivery only
  public string? DeliveryAddress { get; set; }
  public string? DeliveryNote { get; set; }
}

public class CreateAdminOrder(IMediator mediator)
  : Endpoint<CreateAdminOrderRequest, PlaceOrderResponseDto>
{
  public override void Configure()
  {
    Post("/api/admin/orders/create");
    Policies("order.create");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(CreateAdminOrderRequest req, CancellationToken ct)
  {
    var items = req.Items
      .Select(i => new PlaceOrderItemDto(
        i.ProductId, i.ProductName, i.UnitPrice, i.Quantity,
        i.SelectedVariantValueIds,
        i.SelectedOptionValues?.Select(v => new PlaceOrderOptionValueInput(v.OptionValueId, v.Quantity)).ToList(),
        i.IsTakeaway, i.IsFreeGift, i.Note))
      .ToList();

    var result = await mediator.Send(new PlaceOrderCommand(
      req.SessionId, items, req.GuestCount,
      BypassCooldown: true,
      PromoCode: req.PromoCode,
      OrderType: req.OrderType,
      CustomerName: req.CustomerName,
      CustomerPhone: req.CustomerPhone,
      DeliveryAddress: req.DeliveryAddress,
      DeliveryNote: req.DeliveryNote), ct);

    await this.SendResultAsync(result, ct);
  }
}

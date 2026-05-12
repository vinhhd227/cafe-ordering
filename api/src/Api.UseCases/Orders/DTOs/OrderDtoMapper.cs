using Api.Core.Aggregates.OrderAggregate;

namespace Api.UseCases.Orders.DTOs;

public static class OrderDtoMapper
{
  public static OrderItemDto ToDto(this OrderItem i) => new(
    i.Id,
    i.ProductId,
    i.ProductName,
    i.UnitPrice,
    i.OptionAdjustment,
    i.OptionValueTotal,
    i.Quantity,
    i.Discount,
    i.TotalPrice,
    i.SelectedOptions
      .Select(o => new OrderItemOptionDto(o.OptionValueId, o.GroupName, o.Label, o.PriceAdjustment))
      .ToList(),
    i.SelectedOptionValues
      .Select(o => new OrderItemSelectedOptionDto(o.OptionValueId, o.GroupName, o.ValueName, o.UnitPrice, o.Quantity, o.Subtotal))
      .ToList(),
    i.IsTakeaway,
    i.IsFreeGift,
    i.Note);

  public static OrderDto ToOrderDto(
    this Order order,
    string? tableCode,
    bool isManual) => new(
    order.Id,
    order.OrderNumber,
    order.Status.Name.ToUpperInvariant(),
    order.PaymentStatus.Name.ToUpperInvariant(),
    order.PaymentMethod.Name.ToUpperInvariant(),
    order.AmountReceived,
    order.TipAmount,
    order.TotalAmount,
    order.TotalDiscount,
    order.FinalAmount,
    order.OrderDate,
    order.SessionId,
    tableCode,
    order.GuestCount,
    order.CompletedAt,
    order.PaidAt,
    order.Items.Select(i => i.ToDto()).ToList(),
    order.Promotions.Select(p => new AppliedPromotionDto(p.PromotionId, p.PromoCode, p.DiscountAmount)).ToList(),
    isManual);
}

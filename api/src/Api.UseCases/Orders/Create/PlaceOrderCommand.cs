using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Create;

public record PlaceOrderCommand(
  Guid? SessionId,
  List<PlaceOrderItemDto> Items,
  int? GuestCount = null,
  bool BypassCooldown = false,
  string? PromoCode = null,
  string OrderType = "DINE_IN",
  string? CustomerName = null,
  string? CustomerPhone = null,
  string? DeliveryAddress = null,
  string? DeliveryNote = null) : ICommand<Result<PlaceOrderResponseDto>>;

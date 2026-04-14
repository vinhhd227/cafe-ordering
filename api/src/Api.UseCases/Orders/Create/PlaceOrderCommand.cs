using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Create;

public record PlaceOrderCommand(
  Guid SessionId,
  List<PlaceOrderItemDto> Items,
  int? GuestCount = null,
  bool BypassCooldown = false,
  string? PromoCode = null) : ICommand<Result<PlaceOrderResponseDto>>;

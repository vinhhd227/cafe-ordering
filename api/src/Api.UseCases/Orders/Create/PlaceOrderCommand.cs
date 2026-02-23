using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Create;

public record PlaceOrderCommand(
  Guid SessionId,
  List<PlaceOrderItemDto> Items) : ICommand<Result<PlaceOrderResponseDto>>;

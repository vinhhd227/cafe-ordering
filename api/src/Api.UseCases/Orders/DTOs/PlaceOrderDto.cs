namespace Api.UseCases.Orders.DTOs;

public record PlaceOrderItemDto(
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  int Quantity);

public record PlaceOrderResponseDto(
  int OrderId,
  string OrderNumber,
  decimal TotalAmount);

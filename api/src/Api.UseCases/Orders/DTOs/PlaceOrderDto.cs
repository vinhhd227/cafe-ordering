namespace Api.UseCases.Orders.DTOs;

public record PlaceOrderItemDto(
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  int Quantity,
  string? Temperature = null,
  string? IceLevel = null,
  string? SugarLevel = null,
  bool IsTakeaway = false);

public record PlaceOrderResponseDto(
  int OrderId,
  string OrderNumber,
  decimal TotalAmount);

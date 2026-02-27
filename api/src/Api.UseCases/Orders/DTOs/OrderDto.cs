namespace Api.UseCases.Orders.DTOs;

public record OrderItemDto(
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  int Quantity,
  decimal TotalPrice
);

public record OrderDto(
  int Id,
  string OrderNumber,
  string Status,
  string PaymentStatus,
  string PaymentMethod,
  decimal? AmountReceived,
  decimal TipAmount,
  decimal TotalAmount,
  DateTime OrderDate,
  Guid SessionId,
  string? TableCode,
  List<OrderItemDto> Items
);

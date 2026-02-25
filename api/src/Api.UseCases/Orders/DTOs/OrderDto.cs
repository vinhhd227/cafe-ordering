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
  decimal TotalAmount,
  DateTime OrderDate,
  Guid SessionId,
  List<OrderItemDto> Items
);

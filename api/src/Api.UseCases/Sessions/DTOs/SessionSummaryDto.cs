using Api.Core.Aggregates.GuestSessionAggregate;

namespace Api.UseCases.Sessions.DTOs;

public record OrderItemLineDto(
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  int Quantity,
  decimal TotalPrice
);

public record OrderLineDto(
  int OrderId,
  string OrderNumber,
  decimal TotalAmount,
  string Status,
  IReadOnlyList<OrderItemLineDto> Items
);

public record SessionSummaryDto(
  Guid SessionId,
  int? TableId,
  DateTime OpenedAt,
  GuestSessionStatus Status,
  IReadOnlyList<OrderLineDto> Orders,
  decimal GrandTotal
);

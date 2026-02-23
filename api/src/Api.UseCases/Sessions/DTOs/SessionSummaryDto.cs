using Api.Core.Aggregates.GuestSessionAggregate;

namespace Api.UseCases.Sessions.DTOs;

public record OrderLineDto(
  int OrderId,
  string OrderNumber,
  decimal TotalAmount,
  string Status
);

public record SessionSummaryDto(
  Guid SessionId,
  int TableId,
  DateTime OpenedAt,
  GuestSessionStatus Status,
  IReadOnlyList<OrderLineDto> Orders,
  decimal GrandTotal
);

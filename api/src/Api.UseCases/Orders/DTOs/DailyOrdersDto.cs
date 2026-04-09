namespace Api.UseCases.Orders.DTOs;

public record DailyOrderItemSnapshotDto(string Name, int Quantity);

public record DailyOrderDto(
  int OrderId,
  DateTime CreatedAt,
  int? TableNumber,
  List<DailyOrderItemSnapshotDto> Items,
  decimal TotalAmount,
  string PaymentMethod,
  string Status
);

public record DailyOrdersResponseDto(List<DailyOrderDto> Orders);

using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.CreateManual;

public record CreateManualOrderCommand(
  int TableId,
  List<ManualOrderItemDto> Items,
  DateTime? OrderedAt     = null,
  int? GuestCount         = null,
  string Status           = "PENDING",
  string PaymentStatus    = "UNPAID",
  string PaymentMethod    = "UNKNOWN",
  decimal? AmountReceived = null,
  decimal TipAmount       = 0
) : ICommand<Result<OrderDto>>;

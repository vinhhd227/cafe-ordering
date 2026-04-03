using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.UpdateManual;

public record UpdateManualOrderCommand(
  int OrderId,
  List<ManualOrderItemDto> Items,
  DateTime? OrderedAt     = null,
  int? GuestCount         = null,
  string Status           = "PENDING",
  string PaymentStatus    = "UNPAID",
  string PaymentMethod    = "UNKNOWN",
  decimal? AmountReceived = null,
  decimal TipAmount       = 0
) : ICommand<Result<OrderDto>>;

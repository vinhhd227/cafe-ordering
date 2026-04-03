namespace Api.UseCases.Orders.DTOs;

public record ManualOrderItemDto(
  int ProductId,
  int Quantity,
  string? Temperature = null,
  string? IceLevel    = null,
  string? SugarLevel  = null,
  bool IsTakeaway     = false,
  string? Note        = null
);

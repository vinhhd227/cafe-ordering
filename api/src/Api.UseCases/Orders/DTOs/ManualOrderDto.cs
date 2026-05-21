namespace Api.UseCases.Orders.DTOs;

public record ManualOrderItemDto(
  int ProductId,
  int Quantity,
  List<int>? SelectedVariantValueIds = null,
  bool IsTakeaway = false,
  string? Note = null
);

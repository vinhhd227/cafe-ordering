namespace Api.UseCases.Orders.DTOs;

public record PlaceOrderOptionValueInput(int OptionValueId, int Quantity = 1);

public record PlaceOrderItemDto(
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  int Quantity,
  List<int>? SelectedOptionValueIds = null,
  List<PlaceOrderOptionValueInput>? SelectedOptionValues = null,
  bool IsTakeaway = false,
  bool IsFreeGift = false,
  string? Note = null);

public record PlaceOrderResponseDto(
  int OrderId,
  string OrderNumber,
  decimal TotalAmount);

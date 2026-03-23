namespace Api.UseCases.Orders.Edit;

public record EditOrderItemDto(
  int ProductId,
  int Quantity,
  string? Temperature,
  string? IceLevel,
  string? SugarLevel,
  bool IsTakeaway,
  string? Note = null);

public record EditOrderItemsCommand(
  int OrderId,
  IReadOnlyList<EditOrderItemDto> Items,
  int? GuestCount)
  : ICommand<Result>;

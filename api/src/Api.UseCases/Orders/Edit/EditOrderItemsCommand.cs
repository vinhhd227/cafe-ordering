namespace Api.UseCases.Orders.Edit;

public record EditOrderItemDto(
  int ProductId,
  int Quantity,
  List<int>? SelectedOptionValueIds = null,
  bool IsTakeaway = false,
  string? Note = null);

public record EditOrderItemsCommand(
  int OrderId,
  IReadOnlyList<EditOrderItemDto> Items,
  int? GuestCount)
  : ICommand<Result>;

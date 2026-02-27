namespace Api.UseCases.Orders.Split;

public record SplitOrderCommand(int OrderId, IReadOnlyList<SplitItemRequest> Items)
  : ICommand<Result<SplitOrderResult>>;

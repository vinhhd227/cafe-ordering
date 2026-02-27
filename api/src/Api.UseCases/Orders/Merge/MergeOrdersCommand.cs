namespace Api.UseCases.Orders.Merge;

public record MergeOrdersCommand(int PrimaryOrderId, IReadOnlyList<int> SecondaryOrderIds)
  : ICommand<Result>;

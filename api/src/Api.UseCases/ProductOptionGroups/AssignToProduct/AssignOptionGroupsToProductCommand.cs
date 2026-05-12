namespace Api.UseCases.ProductOptionGroups.AssignToProduct;

public record AssignOptionGroupsToProductCommand(
  int ProductId,
  IReadOnlyList<int> GroupIds) : ICommand<Result>;

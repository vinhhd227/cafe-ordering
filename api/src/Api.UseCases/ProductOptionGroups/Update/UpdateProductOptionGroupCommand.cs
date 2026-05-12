using Api.UseCases.ProductOptionGroups.Create;

namespace Api.UseCases.ProductOptionGroups.Update;

public record UpdateProductOptionGroupCommand(
  int Id,
  string Name,
  bool IsRequired,
  bool AllowMultiple,
  bool AllowQuantity,
  IReadOnlyList<ProductOptionValueInput> Values) : ICommand<Result>;

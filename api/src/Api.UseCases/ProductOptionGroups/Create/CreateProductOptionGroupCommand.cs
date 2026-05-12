using Api.UseCases.ProductOptionGroups.DTOs;

namespace Api.UseCases.ProductOptionGroups.Create;

public record ProductOptionValueInput(
  string Name,
  decimal Price,
  decimal? CostPrice);

public record CreateProductOptionGroupCommand(
  string Name,
  bool IsRequired,
  bool AllowMultiple,
  bool AllowQuantity,
  IReadOnlyList<ProductOptionValueInput> Values,
  IReadOnlyList<int> ProductIds) : ICommand<Result<ProductOptionGroupDto>>;

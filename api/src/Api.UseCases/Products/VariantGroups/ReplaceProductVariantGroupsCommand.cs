using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.VariantGroups;

public record VariantValueInput(
  string Label,
  decimal Price,
  bool IsDefault);

public record VariantGroupInput(
  string Name,
  bool IsRequired,
  OptionSelectionType SelectionType,
  IReadOnlyList<VariantValueInput> Values);

public record ReplaceProductVariantGroupsCommand(
  int ProductId,
  IReadOnlyList<VariantGroupInput> Groups)
  : ICommand<Result>;

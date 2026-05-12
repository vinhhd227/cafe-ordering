using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.OptionGroups;

public record AttributeValueInput(
  string Label,
  decimal PriceAdjustment,
  bool IsDefault);

public record AttributeGroupInput(
  string Name,
  bool IsRequired,
  OptionSelectionType SelectionType,
  IReadOnlyList<AttributeValueInput> Values);

public record ReplaceProductAttributeGroupsCommand(
  int ProductId,
  IReadOnlyList<AttributeGroupInput> Groups)
  : ICommand<Result>;

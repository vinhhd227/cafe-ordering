namespace Api.UseCases.Menu.DTOs;

public record MenuAttributeValueDto(int Id, string Label, decimal PriceAdjustment, bool IsDefault);

public record MenuAttributeGroupDto(int Id, string Name, bool IsRequired, string SelectionType, List<MenuAttributeValueDto> Values);

public record MenuOptionValueDto(int Id, string Name, decimal Price);

public record MenuOptionGroupDto(int Id, string Name, bool IsRequired, bool AllowMultiple, bool AllowQuantity, int DisplayOrder, List<MenuOptionValueDto> Values);

public record MenuProductDto(
  int Id,
  string Name,
  string? Description,
  decimal Price,
  string? ImageUrl,
  bool IsAccompaniment,
  int? EstimatedPrepMinutes,
  List<MenuAttributeGroupDto> AttributeGroups,
  List<MenuOptionGroupDto> OptionGroups
);

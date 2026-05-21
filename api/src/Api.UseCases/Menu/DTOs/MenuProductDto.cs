namespace Api.UseCases.Menu.DTOs;

public record MenuVariantValueDto(int Id, string Label, decimal Price, bool IsDefault);

public record MenuVariantGroupDto(int Id, string Name, bool IsRequired, string SelectionType, List<MenuVariantValueDto> Values);

public record MenuOptionValueDto(int Id, string Name, decimal Price);

public record MenuOptionGroupDto(int Id, string Name, bool IsRequired, bool AllowMultiple, bool AllowQuantity, int DisplayOrder, List<MenuOptionValueDto> Values);

public record MenuProductVariantDto(int Id, decimal Price, bool IsActive, List<int> ValueIds);

public record MenuProductDto(
  int Id,
  string Name,
  string? Description,
  decimal Price,
  string? ImageUrl,
  bool IsAccompaniment,
  int? EstimatedPrepMinutes,
  List<MenuVariantGroupDto> VariantGroups,
  List<MenuProductVariantDto> Variants,
  List<MenuOptionGroupDto> OptionGroups
);

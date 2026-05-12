namespace Api.UseCases.Menu.DTOs;

public record AdminMenuProductDto(
  int Id,
  int? CategoryId,
  string Name,
  string? Description,
  decimal Price,
  string? ImageUrl,
  bool IsActive,
  bool IsAccompaniment,
  int? EstimatedPrepMinutes,
  List<MenuAttributeGroupDto> AttributeGroups
);

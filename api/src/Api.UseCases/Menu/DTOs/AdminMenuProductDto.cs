namespace Api.UseCases.Menu.DTOs;

/// <summary>
///   DTO sản phẩm cho admin menu — bao gồm đầy đủ fields để thực hiện toggle isActive.
/// </summary>
public record AdminMenuProductDto(
  int Id,
  int CategoryId,
  string Name,
  string? Description,
  decimal Price,
  string? ImageUrl,
  bool IsActive,
  bool HasTemperatureOption,
  bool HasIceLevelOption,
  bool HasSugarLevelOption
);

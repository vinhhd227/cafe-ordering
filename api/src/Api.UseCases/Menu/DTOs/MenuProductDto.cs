namespace Api.UseCases.Menu.DTOs;

public record MenuProductDto(
  int Id,
  string Name,
  string? Description,
  decimal Price,
  string? ImageUrl,
  bool HasTemperatureOption,
  bool HasIceLevelOption,
  bool HasSugarLevelOption
);

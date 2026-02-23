namespace Api.UseCases.Menu.DTOs;

public record MenuCategoryDto(
  int Id,
  string Name,
  List<MenuProductDto> Products
);

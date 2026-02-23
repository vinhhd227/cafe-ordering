using Api.UseCases.Menu.DTOs;

namespace Api.UseCases.Menu.GetMenu;

public record GetMenuQuery : IQuery<Result<List<MenuCategoryDto>>>;

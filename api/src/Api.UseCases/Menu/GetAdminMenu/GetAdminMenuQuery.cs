using Api.UseCases.Menu.DTOs;

namespace Api.UseCases.Menu.GetAdminMenu;

/// <summary>
///   Query lấy toàn bộ menu cho admin — bao gồm cả inactive categories và products.
/// </summary>
public record GetAdminMenuQuery() : Common.Interfaces.IQuery<Result<List<AdminMenuCategoryDto>>>;

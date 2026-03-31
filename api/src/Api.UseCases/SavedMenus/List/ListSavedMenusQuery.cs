using Api.UseCases.SavedMenus.DTOs;

namespace Api.UseCases.SavedMenus.List;

public record ListSavedMenusQuery : IQuery<Result<List<SavedMenuDto>>>;

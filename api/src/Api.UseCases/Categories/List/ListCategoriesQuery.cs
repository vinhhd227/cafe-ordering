using Api.UseCases.Categories.DTOs;

namespace Api.UseCases.Categories.List;

/// <summary>
///   Query lấy danh sách Categories.
///   ActiveOnly = true chỉ lấy categories đang active.
/// </summary>
public record ListCategoriesQuery(bool ActiveOnly = false) : IQuery<Result<List<CategoryDto>>>;

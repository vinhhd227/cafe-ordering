using Api.UseCases.Categories.DTOs;

namespace Api.UseCases.Categories.Get;

/// <summary>
///   Query lấy chi tiết Category theo Id
/// </summary>
public record GetCategoryQuery(int CategoryId) : IQuery<Result<CategoryDto>>;

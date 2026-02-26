using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.UseCases.Categories.DTOs;

namespace Api.UseCases.Categories.List;

/// <summary>
///   Handler lấy danh sách Categories
/// </summary>
public class ListCategoriesHandler(IReadRepositoryBase<Category> repository)
  : IQueryHandler<ListCategoriesQuery, Result<List<CategoryDto>>>
{
  public async ValueTask<Result<List<CategoryDto>>> Handle(ListCategoriesQuery request, CancellationToken ct)
  {
    var categories = request.ActiveOnly
      ? await repository.ListAsync(new ActiveCategoriesSpec(), ct)
      : await repository.ListAsync(new AllCategoriesSpec(), ct);

    var dtos = categories
      .Select(c => new CategoryDto(
        c.Id,
        c.Name,
        c.Description,
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt))
      .ToList();

    return Result.Success(dtos);
  }
}

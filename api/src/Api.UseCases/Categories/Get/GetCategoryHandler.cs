using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.UseCases.Categories.DTOs;

namespace Api.UseCases.Categories.Get;

/// <summary>
///   Handler lấy chi tiết Category theo Id
/// </summary>
public class GetCategoryHandler(IReadRepositoryBase<Category> repository)
  : Common.Interfaces.IQueryHandler<GetCategoryQuery, Result<CategoryDto>>
{
  public async ValueTask<Result<CategoryDto>> Handle(GetCategoryQuery request, CancellationToken ct)
  {
    var spec = new CategoryByIdSpec(request.CategoryId);
    var category = await repository.FirstOrDefaultAsync(spec, ct);

    if (category is null)
    {
      return Result.NotFound($"Category {request.CategoryId} not found");
    }

    return new CategoryDto(
      category.Id,
      category.Name,
      category.Description,
      category.IsActive,
      category.CreatedAt,
      category.UpdatedAt);
  }
}

using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;

namespace Api.UseCases.Categories.Create;

/// <summary>
///   Handler tạo mới Category
/// </summary>
public class CreateCategoryHandler(IRepositoryBase<Category> repository)
  : ICommandHandler<CreateCategoryCommand, Result<int>>
{
  public async ValueTask<Result<int>> Handle(CreateCategoryCommand request, CancellationToken ct)
  {
    // Kiểm tra trùng tên
    var existingSpec = new CategoryByNameSpec(request.Name);
    var existing = await repository.FirstOrDefaultAsync(existingSpec, ct);

    if (existing is not null)
    {
      return Result.Conflict($"Category \'{request.Name}\' đã tồn tại");
    }

    var category = Category.Create(request.Name);

    await repository.AddAsync(category, ct);

    return Result.Success(category.Id);
  }
}

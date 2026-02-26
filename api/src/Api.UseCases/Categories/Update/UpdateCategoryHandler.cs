using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;

namespace Api.UseCases.Categories.Update;

/// <summary>
///   Handler cập nhật tên Category
/// </summary>
public class UpdateCategoryHandler(IRepositoryBase<Category> repository)
  : Common.Interfaces.ICommandHandler<UpdateCategoryCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
  {
    var category = await repository.GetByIdAsync(request.CategoryId, ct);

    if (category is null)
    {
      return Result.NotFound($"Category {request.CategoryId} not found");
    }

    // Kiểm tra trùng tên
    var existingSpec = new CategoryByNameSpec(request.Name);
    var existing = await repository.FirstOrDefaultAsync(existingSpec, ct);

    if (existing is not null && existing.Id != request.CategoryId)
    {
      return Result.Conflict($"Category \'{request.Name}\' đã tồn tại");
    }

    category.Update(request.Name, request.Description);

    if (request.IsActive && !category.IsActive)
      category.Activate();
    else if (!request.IsActive && category.IsActive)
      category.Deactivate();

    await repository.UpdateAsync(category, ct);

    return Result.Success();
  }
}

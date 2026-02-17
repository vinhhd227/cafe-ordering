using Api.Core.Aggregates.CategoryAggregate;

namespace Api.UseCases.Categories.Delete;

/// <summary>
///   Handler soft delete Category
/// </summary>
public class DeleteCategoryHandler(IRepositoryBase<Category> repository)
  : ICommandHandler<DeleteCategoryCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
  {
    var category = await repository.GetByIdAsync(request.CategoryId, ct);

    if (category is null)
    {
      return Result.NotFound($"Category {request.CategoryId} not found");
    }

    if (category.IsDeleted)
    {
      return Result.Error("Category đã bị xóa trước đó");
    }

    category.Delete(request.DeletedBy);

    await repository.UpdateAsync(category, ct);

    return Result.Success();
  }
}

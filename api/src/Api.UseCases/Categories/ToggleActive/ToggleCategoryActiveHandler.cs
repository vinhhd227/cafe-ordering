using Api.Core.Aggregates.CategoryAggregate;

namespace Api.UseCases.Categories.ToggleActive;

public class ToggleCategoryActiveHandler(IRepositoryBase<Category> repository)
  : ICommandHandler<ToggleCategoryActiveCommand, Result>
{
  public async ValueTask<Result> Handle(ToggleCategoryActiveCommand request, CancellationToken ct)
  {
    var category = await repository.GetByIdAsync(request.CategoryId, ct);

    if (category is null)
      return Result.NotFound($"Category {request.CategoryId} not found.");

    if (category.IsActive)
      category.Deactivate();
    else
      category.Activate();

    await repository.UpdateAsync(category, ct);
    return Result.Success();
  }
}

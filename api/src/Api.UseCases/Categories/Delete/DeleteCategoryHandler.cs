using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Categories.Delete;

/// <summary>
///   Handler soft delete Category
/// </summary>
public class DeleteCategoryHandler(
  IRepositoryBase<Category> repository,
  IRepositoryBase<Product> productRepository)
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

    var products = await productRepository.ListAsync(new ProductsByCategorySpec(request.CategoryId), ct);
    foreach (var product in products)
    {
      product.ChangeCategory(null);
      await productRepository.UpdateAsync(product, ct);
    }

    category.Delete(request.DeletedBy);

    await repository.UpdateAsync(category, ct);

    return Result.Success();
  }
}

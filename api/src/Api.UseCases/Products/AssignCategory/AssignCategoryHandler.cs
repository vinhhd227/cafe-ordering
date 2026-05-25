using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.AssignCategory;

public class AssignCategoryHandler(
  IRepositoryBase<Product> productRepository,
  IReadRepositoryBase<Category> categoryRepository)
  : ICommandHandler<AssignCategoryCommand, Result>
{
  public async ValueTask<Result> Handle(AssignCategoryCommand request, CancellationToken ct)
  {
    var product = await productRepository.GetByIdAsync(request.ProductId, ct);
    if (product is null) return Result.NotFound($"Product {request.ProductId} not found.");

    var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct);
    if (category is null) return Result.NotFound($"Category {request.CategoryId} not found.");

    product.ChangeCategory(request.CategoryId);
    await productRepository.UpdateAsync(product, ct);
    return Result.Success();
  }
}

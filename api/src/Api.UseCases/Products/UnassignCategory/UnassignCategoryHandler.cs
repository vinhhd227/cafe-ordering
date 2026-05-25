using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.UnassignCategory;

public class UnassignCategoryHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<UnassignCategoryCommand, Result>
{
  public async ValueTask<Result> Handle(UnassignCategoryCommand request, CancellationToken ct)
  {
    var product = await repository.GetByIdAsync(request.ProductId, ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    product.ChangeCategory(null);
    await repository.UpdateAsync(product, ct);
    return Result.Success();
  }
}

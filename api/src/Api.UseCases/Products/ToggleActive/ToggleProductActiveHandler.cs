using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.ToggleActive;

public class ToggleProductActiveHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<ToggleProductActiveCommand, Result>
{
  public async ValueTask<Result> Handle(ToggleProductActiveCommand request, CancellationToken ct)
  {
    var product = await repository.GetByIdAsync(request.ProductId, ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    if (product.IsActive)
      product.Deactivate();
    else
      product.Activate();

    await repository.UpdateAsync(product, ct);
    return Result.Success();
  }
}

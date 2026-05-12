using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.ProductOptionGroups.UnlinkFromProduct;

public class UnlinkGroupFromProductHandler(IRepositoryBase<Product> productRepository)
  : ICommandHandler<UnlinkGroupFromProductCommand, Result>
{
  public async ValueTask<Result> Handle(UnlinkGroupFromProductCommand request, CancellationToken ct)
  {
    var product = await productRepository.FirstOrDefaultAsync(
      new ProductByIdWithOptionGroupMappingsSpec(request.ProductId), ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    product.UnassignOptionGroup(request.GroupId);
    await productRepository.UpdateAsync(product, ct);

    return Result.Success();
  }
}

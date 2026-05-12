using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.ProductOptionGroupAggregate;

namespace Api.UseCases.ProductOptionGroups.LinkToProducts;

public class LinkGroupToProductsHandler(
  IRepositoryBase<Product> productRepository,
  IReadRepositoryBase<ProductOptionGroup> groupRepository)
  : ICommandHandler<LinkGroupToProductsCommand, Result>
{
  public async ValueTask<Result> Handle(LinkGroupToProductsCommand request, CancellationToken ct)
  {
    var group = await groupRepository.GetByIdAsync(request.GroupId, ct);
    if (group is null)
      return Result.NotFound($"ProductOptionGroup {request.GroupId} not found.");

    if (request.ProductIds is null || request.ProductIds.Count == 0)
      return Result.Success();

    foreach (var productId in request.ProductIds.Distinct())
    {
      var product = await productRepository.FirstOrDefaultAsync(
        new ProductByIdWithOptionGroupMappingsSpec(productId), ct);

      if (product is null) continue;

      var displayOrder = product.OptionGroupMappings.Count;
      product.AssignOptionGroup(request.GroupId, displayOrder);
      await productRepository.UpdateAsync(product, ct);
    }

    return Result.Success();
  }
}

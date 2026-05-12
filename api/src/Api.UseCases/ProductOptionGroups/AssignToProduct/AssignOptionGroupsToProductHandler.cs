using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

namespace Api.UseCases.ProductOptionGroups.AssignToProduct;

public class AssignOptionGroupsToProductHandler(
  IRepositoryBase<Product> productRepository,
  IReadRepositoryBase<ProductOptionGroup> groupRepository)
  : ICommandHandler<AssignOptionGroupsToProductCommand, Result>
{
  public async ValueTask<Result> Handle(AssignOptionGroupsToProductCommand request, CancellationToken ct)
  {
    var product = await productRepository.FirstOrDefaultAsync(
      new ProductByIdWithOptionGroupMappingsSpec(request.ProductId), ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    if (request.GroupIds.Count > 0)
    {
      // Validate tất cả groupIds tồn tại
      var existing = await groupRepository.ListAsync(
        new ProductOptionGroupsByIdsSpec(request.GroupIds), ct);

      if (existing.Count != request.GroupIds.Distinct().Count())
      {
        var missing = request.GroupIds.Except(existing.Select(g => g.Id)).First();
        return Result.NotFound($"ProductOptionGroup {missing} not found.");
      }
    }

    product.ReplaceOptionGroupMappings(request.GroupIds);
    await productRepository.UpdateAsync(product, ct);

    return Result.Success();
  }
}

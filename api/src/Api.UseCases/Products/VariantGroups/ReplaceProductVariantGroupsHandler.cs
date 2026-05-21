using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Products.VariantGroups;

public class ReplaceProductVariantGroupsHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<ReplaceProductVariantGroupsCommand, Result>
{
  public async ValueTask<Result> Handle(ReplaceProductVariantGroupsCommand request, CancellationToken ct)
  {
    var spec = new ProductByIdWithCategorySpec(request.ProductId);
    var product = await repository.FirstOrDefaultAsync(spec, ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    var groupData = request.Groups
      .Select(g => new ProductVariantGroupData(
        g.Name,
        g.IsRequired,
        g.SelectionType,
        g.Values
          .Select(v => new ProductVariantValueData(v.Label, v.Price, v.IsDefault))
          .ToList()))
      .ToList();

    product.ReplaceVariantGroups(groupData);

    await repository.UpdateAsync(product, ct);

    return Result.Success();
  }
}

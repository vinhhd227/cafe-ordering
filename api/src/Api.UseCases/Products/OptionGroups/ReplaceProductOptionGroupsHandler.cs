using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Products.OptionGroups;

public class ReplaceProductAttributeGroupsHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<ReplaceProductAttributeGroupsCommand, Result>
{
  public async ValueTask<Result> Handle(ReplaceProductAttributeGroupsCommand request, CancellationToken ct)
  {
    var spec = new ProductByIdWithCategorySpec(request.ProductId);
    var product = await repository.FirstOrDefaultAsync(spec, ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    var groupData = request.Groups
      .Select(g => new ProductAttributeGroupData(
        g.Name,
        g.IsRequired,
        g.SelectionType,
        g.Values
          .Select(v => new ProductAttributeValueData(v.Label, v.PriceAdjustment, v.IsDefault))
          .ToList()))
      .ToList();

    product.ReplaceAttributeGroups(groupData);

    await repository.UpdateAsync(product, ct);

    return Result.Success();
  }
}

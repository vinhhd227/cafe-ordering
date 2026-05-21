using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;
using Api.UseCases.Products.Variants;

namespace Api.UseCases.Products.Update;

public class UpdateProductHandler(
  IRepositoryBase<Product> repository,
  IReadRepositoryBase<ProductOptionGroup> optionGroupRepository)
  : ICommandHandler<UpdateProductCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateProductCommand request, CancellationToken ct)
  {
    var product = await repository.FirstOrDefaultAsync(
      new ProductByIdWithCategorySpec(request.ProductId), ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found");

    product.ChangeCategory(request.CategoryId);

    product.UpdateDetails(
      request.Name,
      request.Price,
      request.Description,
      request.ImageUrl);

    product.UpdateAccompaniment(request.IsAccompaniment);
    product.SetEstimatedPrepTime(request.EstimatedPrepMinutes);
    product.SetCostPrice(request.CostPrice);
    product.SetDiscountPrice(request.DiscountPrice);
    product.SetSku(request.Sku);
    product.SetBarcode(request.Barcode);

    if (request.IsActive)
      product.Activate();
    else
      product.Deactivate();

    if (request.VariantGroups is not null)
    {
      var groupData = request.VariantGroups
        .Select(g => new ProductVariantGroupData(
          g.Name,
          g.IsRequired,
          g.SelectionType,
          g.Values
            .Select(v => new ProductVariantValueData(v.Label, v.Price, v.IsDefault))
            .ToList()))
        .ToList();

      product.ReplaceVariantGroups(groupData);
    }

    if (request.AssignedOptionGroupIds is not null)
    {
      if (request.AssignedOptionGroupIds.Count > 0)
      {
        var existing = await optionGroupRepository.ListAsync(
          new ProductOptionGroupsByIdsSpec(request.AssignedOptionGroupIds), ct);

        if (existing.Count != request.AssignedOptionGroupIds.Distinct().Count())
        {
          var missing = request.AssignedOptionGroupIds.Except(existing.Select(g => g.Id)).First();
          return Result.NotFound($"ProductOptionGroup {missing} not found.");
        }
      }

      product.ReplaceOptionGroupMappings(request.AssignedOptionGroupIds);
    }

    await repository.UpdateAsync(product, ct);

    if (request.Variants is { Count: > 0 })
    {
      var saved = await repository.FirstOrDefaultAsync(
        new ProductByIdWithCategorySpec(request.ProductId), ct);

      if (saved is null)
        return Result.NotFound($"Product {request.ProductId} not found");

      var variantData = ProductVariantLabelResolver.Resolve(saved, request.Variants);
      if (!variantData.IsSuccess)
        return Result.Invalid(variantData.ValidationErrors);

      saved.ReplaceVariants(variantData.Value);
      await repository.UpdateAsync(saved, ct);
    }

    return Result.Success();
  }
}

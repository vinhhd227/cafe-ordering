using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.UseCases.ProductOptionGroups.DTOs;

namespace Api.UseCases.ProductOptionGroups.Create;

public class CreateProductOptionGroupHandler(
  IRepositoryBase<ProductOptionGroup> repository,
  IRepositoryBase<Product> productRepository)
  : ICommandHandler<CreateProductOptionGroupCommand, Result<ProductOptionGroupDto>>
{
  public async ValueTask<Result<ProductOptionGroupDto>> Handle(
    CreateProductOptionGroupCommand request, CancellationToken ct)
  {
    var group = ProductOptionGroup.Create(
      request.Name,
      request.IsRequired,
      request.AllowMultiple,
      request.AllowQuantity);

    if (request.Values.Count > 0)
    {
      var valueData = request.Values
        .Select(v => new ProductOptionValueData(v.Name, v.Price, v.CostPrice))
        .ToList();
      group.ReplaceValues(valueData);
    }

    await repository.AddAsync(group, ct);

    if (request.ProductIds is { Count: > 0 })
    {
      foreach (var productId in request.ProductIds.Distinct())
      {
        var product = await productRepository.FirstOrDefaultAsync(
          new ProductByIdWithOptionGroupMappingsSpec(productId), ct);

        if (product is null) continue;

        product.AssignOptionGroup(group.Id, product.OptionGroupMappings.Count);
        await productRepository.UpdateAsync(product, ct);
      }
    }

    return Result.Success(group.ToDto());
  }
}

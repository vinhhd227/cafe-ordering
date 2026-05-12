using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;
using Api.UseCases.ProductOptionGroups.DTOs;

namespace Api.UseCases.ProductOptionGroups.Get;

public class GetProductOptionGroupHandler(
  IReadRepositoryBase<ProductOptionGroup> repository,
  IReadRepositoryBase<Product> productRepository)
  : IQueryHandler<GetProductOptionGroupQuery, Result<ProductOptionGroupDto>>
{
  public async ValueTask<Result<ProductOptionGroupDto>> Handle(
    GetProductOptionGroupQuery request, CancellationToken ct)
  {
    var spec = new ProductOptionGroupByIdWithValuesSpec(request.Id);
    var group = await repository.FirstOrDefaultAsync(spec, ct);

    if (group is null)
      return Result.NotFound($"ProductOptionGroup {request.Id} not found.");

    var productIds = group.Mappings.Select(m => m.ProductId).ToList();
    List<LinkedProductDto> linkedProducts = [];

    if (productIds.Count > 0)
    {
      var products = await productRepository.ListAsync(new ProductsByIdsSpec(productIds), ct);
      linkedProducts = products
        .Select(p => new LinkedProductDto(p.Id, p.Name, p.Price, p.ImageUrl))
        .ToList();
    }

    return Result.Success(group.ToDto(linkedProducts));
  }
}

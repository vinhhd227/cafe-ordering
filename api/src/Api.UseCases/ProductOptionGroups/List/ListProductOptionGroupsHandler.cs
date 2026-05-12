using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;
using Api.UseCases.ProductOptionGroups.DTOs;

namespace Api.UseCases.ProductOptionGroups.List;

public class ListProductOptionGroupsHandler(IReadRepositoryBase<ProductOptionGroup> repository)
  : IQueryHandler<ListProductOptionGroupsQuery, Result<List<ProductOptionGroupSummaryDto>>>
{
  public async ValueTask<Result<List<ProductOptionGroupSummaryDto>>> Handle(
    ListProductOptionGroupsQuery request, CancellationToken ct)
  {
    var spec = new ProductOptionGroupListSpec(request.SearchTerm, request.IsActive);
    var groups = await repository.ListAsync(spec, ct);

    var dtos = groups.Select(g => new ProductOptionGroupSummaryDto(
      g.Id,
      g.Name,
      g.IsRequired,
      g.AllowMultiple,
      g.AllowQuantity,
      g.IsActive,
      g.Values.Count,
      g.Mappings.Count,
      g.Values.OrderBy(v => v.DisplayOrder).Select(v => v.Name).ToList())).ToList();

    return Result.Success(dtos);
  }
}

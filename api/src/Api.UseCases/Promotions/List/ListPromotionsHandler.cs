using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.Create;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.List;

public class ListPromotionsHandler(IReadRepositoryBase<Promotion> repo)
  : IQueryHandler<ListPromotionsQuery, Result<PagedPromotionsDto>>
{
  public async ValueTask<Result<PagedPromotionsDto>> Handle(ListPromotionsQuery request, CancellationToken ct)
  {
    var countSpec = new PromotionsCountSpec(request.IsActive, request.Scope, request.DiscountType);
    var listSpec  = new PromotionsListSpec(request.IsActive, request.Scope, request.DiscountType,
      request.Page, request.PageSize);

    var total     = await repo.CountAsync(countSpec, ct);
    var promos    = await repo.ListAsync(listSpec, ct);

    var dtos = promos.Select(CreatePromotionHandler.ToDto).ToList();

    return Result.Success(new PagedPromotionsDto(dtos, total, request.Page, request.PageSize));
  }
}

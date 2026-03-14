using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.Create;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.ListPublic;

public class ListPublicPromotionsHandler(IReadRepositoryBase<Promotion> repo)
  : IQueryHandler<ListPublicPromotionsQuery, Result<List<PromotionDto>>>
{
  public async ValueTask<Result<List<PromotionDto>>> Handle(ListPublicPromotionsQuery request, CancellationToken ct)
  {
    var promos = await repo.ListAsync(new PublicActivePromotionsSpec(DateTime.UtcNow), ct);
    return Result.Success(promos.Select(CreatePromotionHandler.ToDto).ToList());
  }
}

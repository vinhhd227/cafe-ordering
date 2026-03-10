using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.Create;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Get;

public class GetPromotionHandler(IReadRepositoryBase<Promotion> repo)
  : IQueryHandler<GetPromotionQuery, Result<PromotionDto>>
{
  public async ValueTask<Result<PromotionDto>> Handle(GetPromotionQuery request, CancellationToken ct)
  {
    Promotion? promo = null;

    if (request.Id.HasValue)
      promo = await repo.FirstOrDefaultAsync(new PromotionByIdSpec(request.Id.Value), ct);
    else if (!string.IsNullOrWhiteSpace(request.Code))
      promo = await repo.FirstOrDefaultAsync(new PromotionByCodeSpec(request.Code), ct);

    if (promo is null)
      return Result.NotFound("Promotion not found.");

    return Result.Success(CreatePromotionHandler.ToDto(promo));
  }
}

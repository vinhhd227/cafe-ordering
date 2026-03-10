using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.Create;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Toggle;

public class TogglePromotionHandler(IRepositoryBase<Promotion> repo)
  : ICommandHandler<TogglePromotionCommand, Result<PromotionDto>>
{
  public async ValueTask<Result<PromotionDto>> Handle(TogglePromotionCommand cmd, CancellationToken ct)
  {
    var promo = await repo.FirstOrDefaultAsync(new PromotionByIdSpec(cmd.Id), ct);
    if (promo is null)
      return Result.NotFound($"Promotion {cmd.Id} not found.");

    if (cmd.Activate) promo.Activate();
    else              promo.Deactivate();

    await repo.UpdateAsync(promo, ct);

    return Result.Success(CreatePromotionHandler.ToDto(promo));
  }
}

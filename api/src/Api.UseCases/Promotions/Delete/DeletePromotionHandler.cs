using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;

namespace Api.UseCases.Promotions.Delete;

public class DeletePromotionHandler(IRepositoryBase<Promotion> repo)
  : ICommandHandler<DeletePromotionCommand, Result>
{
  public async ValueTask<Result> Handle(DeletePromotionCommand cmd, CancellationToken ct)
  {
    var promo = await repo.FirstOrDefaultAsync(new PromotionByIdSpec(cmd.Id), ct);
    if (promo is null)
      return Result.NotFound($"Promotion {cmd.Id} not found.");

    promo.Delete(cmd.DeletedBy);
    await repo.UpdateAsync(promo, ct);

    return Result.Success();
  }
}

using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.Create;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Update;

public class UpdatePromotionHandler(IRepositoryBase<Promotion> repo)
  : ICommandHandler<UpdatePromotionCommand, Result<PromotionDto>>
{
  public async ValueTask<Result<PromotionDto>> Handle(UpdatePromotionCommand cmd, CancellationToken ct)
  {
    var promo = await repo.FirstOrDefaultAsync(new PromotionByIdSpec(cmd.Id), ct);
    if (promo is null)
      return Result.NotFound($"Promotion {cmd.Id} not found.");

    // Unique code check (exclude self)
    if (!string.IsNullOrWhiteSpace(cmd.Code))
    {
      var existing = await repo.FirstOrDefaultAsync(new PromotionByCodeSpec(cmd.Code), ct);
      if (existing is not null && existing.Id != cmd.Id)
        return Result.Invalid(new ValidationError("Code", "A promotion with this code already exists."));
    }

    DiscountType  discountType = DiscountType.FromName(cmd.DiscountType, true);
    PromotionScope scope       = PromotionScope.FromName(cmd.Scope, true);
    StackPolicy   stackPolicy  = StackPolicy.FromName(cmd.StackPolicy, true);

    promo.Update(
      cmd.Name, cmd.Code, cmd.Description,
      discountType, cmd.DiscountValue, cmd.BuyQuantity, cmd.GetQuantity,
      scope, cmd.ApplicableProductIds, cmd.ApplicableCategoryIds,
      stackPolicy, cmd.MinOrderAmount,
      DateTime.SpecifyKind(cmd.StartDate, DateTimeKind.Utc),
      cmd.EndDate.HasValue ? DateTime.SpecifyKind(cmd.EndDate.Value, DateTimeKind.Utc) : null,
      cmd.MaxUsage);

    await repo.UpdateAsync(promo, ct);

    return Result.Success(CreatePromotionHandler.ToDto(promo));
  }
}

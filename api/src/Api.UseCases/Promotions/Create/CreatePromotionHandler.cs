using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Create;

public class CreatePromotionHandler(IRepositoryBase<Promotion> repo)
  : ICommandHandler<CreatePromotionCommand, Result<PromotionDto>>
{
  public async ValueTask<Result<PromotionDto>> Handle(CreatePromotionCommand cmd, CancellationToken ct)
  {
    // Unique code check
    if (!string.IsNullOrWhiteSpace(cmd.Code))
    {
      var existing = await repo.FirstOrDefaultAsync(new PromotionByCodeSpec(cmd.Code), ct);
      if (existing is not null)
        return Result.Invalid(new ValidationError("Code", "A promotion with this code already exists."));
    }

    DiscountType  discountType = DiscountType.FromName(cmd.DiscountType, true);
    PromotionScope scope       = PromotionScope.FromName(cmd.Scope, true);
    StackPolicy   stackPolicy  = StackPolicy.FromName(cmd.StackPolicy, true);

    var promo = Promotion.Create(
      cmd.Name, cmd.Code, cmd.Description,
      discountType, cmd.DiscountValue, cmd.BuyQuantity, cmd.GetQuantity,
      scope, cmd.ApplicableProductIds, cmd.ApplicableCategoryIds,
      stackPolicy, cmd.MinOrderAmount,
      DateTime.SpecifyKind(cmd.StartDate, DateTimeKind.Utc),
      cmd.EndDate.HasValue ? DateTime.SpecifyKind(cmd.EndDate.Value, DateTimeKind.Utc) : null,
      cmd.MaxUsage);

    await repo.AddAsync(promo, ct);

    return Result.Success(ToDto(promo));
  }

  internal static PromotionDto ToDto(Promotion p) => new(
    p.Id, p.Name, p.Code, p.Description,
    p.DiscountType.Name.ToUpperInvariant(),
    p.DiscountValue, p.BuyQuantity, p.GetQuantity,
    p.Scope.Name.ToUpperInvariant(),
    p.ApplicableProductIds, p.ApplicableCategoryIds,
    p.StackPolicy.Name.ToUpperInvariant(),
    p.MinOrderAmount, p.StartDate, p.EndDate,
    p.MaxUsage, p.CurrentUsage, p.IsActive,
    p.CreatedAt, p.UpdatedAt);
}

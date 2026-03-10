using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;

namespace Api.UseCases.Promotions.Validate;

public class ValidatePromotionHandler(IReadRepositoryBase<Promotion> repo)
  : IQueryHandler<ValidatePromotionQuery, Result<ValidatePromotionResult>>
{
  public async ValueTask<Result<ValidatePromotionResult>> Handle(
    ValidatePromotionQuery request, CancellationToken ct)
  {
    var promo = await repo.FirstOrDefaultAsync(new PromotionByCodeSpec(request.Code), ct);
    if (promo is null)
      return Result.NotFound($"Promotion with code '{request.Code}' not found.");

    var now = DateTime.UtcNow;

    if (!promo.IsValidAt(now))
      return Result.Success(new ValidatePromotionResult(
        promo.Id, promo.Name, promo.Code,
        promo.DiscountType.Name, promo.DiscountValue,
        promo.Scope.Name, promo.StackPolicy.Name,
        promo.MinOrderAmount, promo.StartDate, promo.EndDate,
        null, false, "Promotion is not active or has expired."));

    if (!promo.HasUsageLeft())
      return Result.Success(new ValidatePromotionResult(
        promo.Id, promo.Name, promo.Code,
        promo.DiscountType.Name, promo.DiscountValue,
        promo.Scope.Name, promo.StackPolicy.Name,
        promo.MinOrderAmount, promo.StartDate, promo.EndDate,
        null, false, "Promotion has reached its usage limit."));

    // Check MinOrderAmount if orderAmount provided
    bool isApplicable = true;
    string? message   = null;
    decimal? estimatedDiscount = null;

    if (request.OrderAmount.HasValue)
    {
      if (!promo.IsApplicableTo(request.OrderAmount.Value))
      {
        isApplicable = false;
        message = $"Order total must be at least {promo.MinOrderAmount:N0} to apply this promotion.";
      }
      else if (promo.Scope == PromotionScope.Order && promo.DiscountType != DiscountType.BuyXGetY)
      {
        // Estimate ORDER scope discount (PRODUCT/CATEGORY requires item info)
        estimatedDiscount = promo.DiscountType == DiscountType.Percentage
          ? Math.Round(request.OrderAmount.Value * promo.DiscountValue / 100, 0)
          : Math.Min(promo.DiscountValue, request.OrderAmount.Value);
      }
    }

    return Result.Success(new ValidatePromotionResult(
      promo.Id, promo.Name, promo.Code,
      promo.DiscountType.Name, promo.DiscountValue,
      promo.Scope.Name, promo.StackPolicy.Name,
      promo.MinOrderAmount, promo.StartDate, promo.EndDate,
      estimatedDiscount, isApplicable, message));
  }
}

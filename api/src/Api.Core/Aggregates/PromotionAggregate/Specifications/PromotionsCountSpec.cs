namespace Api.Core.Aggregates.PromotionAggregate.Specifications;

public class PromotionsCountSpec : Specification<Promotion>
{
  public PromotionsCountSpec(
    bool? isActive = null,
    string? scope = null,
    string? discountType = null)
  {
    Query.Where(p => !p.IsDeleted);

    if (isActive.HasValue)
      Query.Where(p => p.IsActive == isActive.Value);

    if (!string.IsNullOrWhiteSpace(scope))
    {
      var target = PromotionScope.FromName(scope, true);
      Query.Where(p => p.Scope == target);
    }

    if (!string.IsNullOrWhiteSpace(discountType))
    {
      var target = DiscountType.FromName(discountType, true);
      Query.Where(p => p.DiscountType == target);
    }
  }
}

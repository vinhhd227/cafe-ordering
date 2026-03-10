namespace Api.Core.Aggregates.PromotionAggregate.Specifications;

public class PromotionsListSpec : Specification<Promotion>
{
  public PromotionsListSpec(
    bool? isActive = null,
    string? scope = null,
    string? discountType = null,
    int page = 1,
    int pageSize = 20)
  {
    Query.Where(p => !p.IsDeleted);
    Query.OrderByDescending(p => p.StartDate);

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

    Query.Skip((page - 1) * pageSize).Take(pageSize);
  }
}

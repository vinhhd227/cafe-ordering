namespace Api.Core.Aggregates.PromotionAggregate.Specifications;

public class PromotionByCodeSpec : SingleResultSpecification<Promotion>
{
  public PromotionByCodeSpec(string code) =>
    Query.Where(p => p.Code == code.ToUpperInvariant() && !p.IsDeleted);
}

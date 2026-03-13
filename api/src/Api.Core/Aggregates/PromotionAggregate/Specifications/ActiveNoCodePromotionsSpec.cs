namespace Api.Core.Aggregates.PromotionAggregate.Specifications;

public class ActiveNoCodePromotionsSpec : Specification<Promotion>
{
  public ActiveNoCodePromotionsSpec() =>
    Query.Where(p => !p.IsDeleted && p.IsActive && p.Code == null);
}

namespace Api.Core.Aggregates.PromotionAggregate.Specifications;

public class PromotionByIdSpec : SingleResultSpecification<Promotion>
{
  public PromotionByIdSpec(int id) =>
    Query.Where(p => p.Id == id && !p.IsDeleted);
}

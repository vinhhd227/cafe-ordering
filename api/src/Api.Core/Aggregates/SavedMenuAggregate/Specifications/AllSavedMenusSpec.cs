namespace Api.Core.Aggregates.SavedMenuAggregate.Specifications;

public class AllSavedMenusSpec : Specification<SavedMenu>
{
  public AllSavedMenusSpec()
  {
    Query.Where(m => !m.IsDeleted);
    Query.OrderByDescending(m => m.CreatedAt);
  }
}

namespace Api.Core.Aggregates.SavedMenuAggregate.Specifications;

public class SavedMenuByIdSpec : Specification<SavedMenu>
{
  public SavedMenuByIdSpec(int id)
  {
    Query.Where(m => m.Id == id && !m.IsDeleted);
  }
}

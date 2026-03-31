namespace Api.Core.Aggregates.WifiProfileAggregate.Specifications;

public class WifiProfileByIdSpec : Specification<WifiProfile>
{
  public WifiProfileByIdSpec(int id)
  {
    Query.Where(w => w.Id == id && !w.IsDeleted);
  }
}

namespace Api.Core.Aggregates.WifiProfileAggregate.Specifications;

public class AllWifiProfilesSpec : Specification<WifiProfile>
{
  public AllWifiProfilesSpec()
  {
    Query.Where(w => !w.IsDeleted);
    Query.OrderBy(w => w.Name);
  }
}

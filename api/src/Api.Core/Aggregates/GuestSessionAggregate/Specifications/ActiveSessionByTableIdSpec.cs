namespace Api.Core.Aggregates.GuestSessionAggregate.Specifications;

public class ActiveSessionByTableIdSpec : Specification<GuestSession>
{
  public ActiveSessionByTableIdSpec(int tableId)
  {
    Query.Where(s => s.TableId == tableId && s.Status == GuestSessionStatus.Active);
  }
}

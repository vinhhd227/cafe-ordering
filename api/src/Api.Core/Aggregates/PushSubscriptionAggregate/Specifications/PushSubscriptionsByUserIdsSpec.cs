namespace Api.Core.Aggregates.PushSubscriptionAggregate.Specifications;

public class PushSubscriptionsByUserIdsSpec : Specification<PushSubscription>
{
    public PushSubscriptionsByUserIdsSpec(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        Query.Where(s => ids.Contains(s.UserId));
    }
}

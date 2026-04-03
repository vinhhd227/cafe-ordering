namespace Api.Core.Aggregates.PushSubscriptionAggregate.Specifications;

public class PushSubscriptionByEndpointSpec : SingleResultSpecification<PushSubscription>
{
    public PushSubscriptionByEndpointSpec(string endpoint)
        => Query.Where(s => s.Endpoint == endpoint);
}

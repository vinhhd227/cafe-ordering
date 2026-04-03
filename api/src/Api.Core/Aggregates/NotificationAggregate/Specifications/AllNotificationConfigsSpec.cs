namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class AllNotificationConfigsSpec : Specification<NotificationConfig>
{
    public AllNotificationConfigsSpec()
    {
        Query.OrderBy(c => c.Type);
    }
}

namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class OldNotificationsSpec : Specification<Notification>
{
    public OldNotificationsSpec(DateTime before)
    {
        Query.Where(n => n.CreatedAt < before);
    }
}

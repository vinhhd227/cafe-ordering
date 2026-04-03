namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class NotificationsCountByUserSpec : Specification<Notification>
{
    public NotificationsCountByUserSpec(string userId)
    {
        Query.Where(n => n.UserId == userId);
    }
}

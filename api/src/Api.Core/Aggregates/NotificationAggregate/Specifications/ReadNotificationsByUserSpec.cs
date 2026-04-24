namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class ReadNotificationsByUserSpec : Specification<Notification>
{
    public ReadNotificationsByUserSpec(string userId)
    {
        Query.Where(n => n.UserId == userId && n.IsRead);
    }
}

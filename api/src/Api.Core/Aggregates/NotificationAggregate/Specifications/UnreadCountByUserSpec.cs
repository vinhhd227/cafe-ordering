namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class UnreadCountByUserSpec : Specification<Notification>
{
    public UnreadCountByUserSpec(string userId)
    {
        Query.Where(n => n.UserId == userId && !n.IsRead);
    }
}

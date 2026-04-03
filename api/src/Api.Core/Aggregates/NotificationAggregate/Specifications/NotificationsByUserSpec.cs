namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class NotificationsByUserSpec : Specification<Notification>
{
    public NotificationsByUserSpec(string userId, int page, int pageSize)
    {
        Query
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}

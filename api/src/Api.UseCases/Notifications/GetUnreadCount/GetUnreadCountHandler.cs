using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;

namespace Api.UseCases.Notifications.GetUnreadCount;

public class GetUnreadCountHandler(IReadRepositoryBase<Notification> repo)
    : IQueryHandler<GetUnreadCountQuery, Result<int>>
{
    public async ValueTask<Result<int>> Handle(GetUnreadCountQuery q, CancellationToken ct)
    {
        var count = await repo.CountAsync(new UnreadCountByUserSpec(q.UserId), ct);
        return Result.Success(count);
    }
}

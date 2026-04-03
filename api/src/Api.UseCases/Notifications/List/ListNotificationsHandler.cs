using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;
using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.Notifications.List;

public class ListNotificationsHandler(IReadRepositoryBase<Notification> repo)
    : IQueryHandler<ListNotificationsQuery, Result<NotificationListDto>>
{
    public async ValueTask<Result<NotificationListDto>> Handle(ListNotificationsQuery q, CancellationToken ct)
    {
        var items = await repo.ListAsync(new NotificationsByUserSpec(q.UserId, q.Page, q.PageSize), ct);
        var total = await repo.CountAsync(new NotificationsCountByUserSpec(q.UserId), ct);
        var unread = await repo.CountAsync(new UnreadCountByUserSpec(q.UserId), ct);

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.Type.Name, n.Title, n.Body, n.Url,
            n.IsRead, n.ReadAt, n.ReferenceId, n.CreatedAt));

        return Result.Success(new NotificationListDto(dtos, total, unread));
    }
}

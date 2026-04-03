using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;

namespace Api.UseCases.Notifications.MarkAllRead;

public class MarkAllNotificationsReadHandler(IRepositoryBase<Notification> repo)
    : ICommandHandler<MarkAllNotificationsReadCommand, Result>
{
    public async ValueTask<Result> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        var unread = await repo.ListAsync(new UnreadCountByUserSpec(cmd.UserId), ct);
        foreach (var n in unread) n.MarkRead();
        await repo.UpdateRangeAsync(unread, ct);
        return Result.Success();
    }
}

using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;

namespace Api.UseCases.Notifications.DeleteRead;

public class DeleteReadNotificationsHandler(IRepositoryBase<Notification> repo)
    : ICommandHandler<DeleteReadNotificationsCommand, Result<int>>
{
    public async ValueTask<Result<int>> Handle(DeleteReadNotificationsCommand cmd, CancellationToken ct)
    {
        var read = await repo.ListAsync(new ReadNotificationsByUserSpec(cmd.UserId), ct);
        if (read.Count == 0) return Result.Success(0);

        await repo.DeleteRangeAsync(read, ct);
        return Result.Success(read.Count);
    }
}

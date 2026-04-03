using Api.Core.Aggregates.NotificationAggregate;

namespace Api.UseCases.Notifications.MarkRead;

public class MarkNotificationReadHandler(IRepositoryBase<Notification> repo)
    : ICommandHandler<MarkNotificationReadCommand, Result>
{
    public async ValueTask<Result> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var notification = await repo.GetByIdAsync(cmd.Id, ct);
        if (notification is null) return Result.NotFound();
        if (notification.UserId != cmd.UserId) return Result.Forbidden();

        notification.MarkRead();
        await repo.UpdateAsync(notification, ct);
        return Result.Success();
    }
}

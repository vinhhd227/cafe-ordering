using Api.Core.Aggregates.NotificationAggregate;

namespace Api.UseCases.Notifications.Delete;

public class DeleteNotificationHandler(IRepositoryBase<Notification> repo)
    : ICommandHandler<DeleteNotificationCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteNotificationCommand cmd, CancellationToken ct)
    {
        var notification = await repo.GetByIdAsync(cmd.Id, ct);
        if (notification is null) return Result.NotFound();
        if (notification.UserId != cmd.UserId) return Result.Forbidden();

        await repo.DeleteAsync(notification, ct);
        return Result.Success();
    }
}

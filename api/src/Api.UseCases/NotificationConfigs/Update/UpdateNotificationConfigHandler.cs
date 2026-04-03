using Api.Core.Aggregates.NotificationAggregate;
using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.Update;

public class UpdateNotificationConfigHandler(IRepositoryBase<NotificationConfig> repo)
    : ICommandHandler<UpdateNotificationConfigCommand, Result<NotificationConfigDto>>
{
    public async ValueTask<Result<NotificationConfigDto>> Handle(
        UpdateNotificationConfigCommand cmd, CancellationToken ct)
    {
        var config = await repo.GetByIdAsync(cmd.Id, ct);
        if (config is null) return Result.NotFound();

        config.Update(cmd.TargetRoles, cmd.IsEnabled);
        await repo.UpdateAsync(config, ct);

        return Result.Success(new NotificationConfigDto(config.Id, config.Type, config.TargetRoles, config.IsEnabled));
    }
}

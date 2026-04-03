using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;
using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.List;

public class ListNotificationConfigsHandler(IReadRepositoryBase<NotificationConfig> repo)
    : IQueryHandler<ListNotificationConfigsQuery, Result<IEnumerable<NotificationConfigDto>>>
{
    public async ValueTask<Result<IEnumerable<NotificationConfigDto>>> Handle(
        ListNotificationConfigsQuery q, CancellationToken ct)
    {
        var configs = await repo.ListAsync(new AllNotificationConfigsSpec(), ct);
        var dtos = configs.Select(c => new NotificationConfigDto(c.Id, c.Type, c.TargetRoles, c.IsEnabled));
        return Result.Success(dtos);
    }
}

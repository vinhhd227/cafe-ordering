using Api.UseCases.Notifications.DTOs;
using Api.UseCases.NotificationConfigs.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.NotificationConfigs;

public class ListNotificationConfigsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<IEnumerable<NotificationConfigDto>>
{
    public override void Configure()
    {
        Get("/api/admin/notification-configs");
        Policies("admin.access", "notification.config");
        DontAutoTag();
        Description(b => b.WithTags("NotificationConfigs"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListNotificationConfigsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}

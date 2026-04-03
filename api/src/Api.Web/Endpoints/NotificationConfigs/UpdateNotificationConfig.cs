using Api.UseCases.Notifications.DTOs;
using Api.UseCases.NotificationConfigs.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.NotificationConfigs;

public class UpdateNotificationConfigEndpoint(IMediator mediator)
    : Endpoint<UpdateNotificationConfigRequest, NotificationConfigDto>
{
    public override void Configure()
    {
        Put("/api/admin/notification-configs/{id}");
        Policies("AdminOnly");
        DontAutoTag();
        Description(b => b.WithTags("NotificationConfigs"));
    }

    public override async Task HandleAsync(UpdateNotificationConfigRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateNotificationConfigCommand(req.Id, req.TargetRoles, req.IsEnabled), ct);
        await this.SendResultAsync(result, ct);
    }
}

public class UpdateNotificationConfigRequest
{
    public int Id { get; set; }
    public List<string> TargetRoles { get; set; } = [];
    public bool IsEnabled { get; set; }
}

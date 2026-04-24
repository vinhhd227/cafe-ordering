using System.Security.Claims;
using Api.UseCases.Notifications.DeleteRead;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Notifications;

public class DeleteReadNotificationsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<int>
{
    public override void Configure()
    {
        Delete("/api/admin/notifications/read");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new DeleteReadNotificationsCommand(userId), ct);
        await this.SendResultAsync(result, ct);
    }
}

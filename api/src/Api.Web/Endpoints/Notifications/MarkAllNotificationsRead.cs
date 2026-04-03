using System.Security.Claims;
using Api.UseCases.Notifications.MarkAllRead;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Notifications;

public class MarkAllNotificationsReadEndpoint(IMediator mediator) : Ep.Req<EmptyRequest>.NoRes
{
    public override void Configure()
    {
        Put("/api/admin/notifications/read-all");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Notifications"));
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new MarkAllNotificationsReadCommand(userId), ct);
        await this.SendResultAsync(result, ct);
    }
}

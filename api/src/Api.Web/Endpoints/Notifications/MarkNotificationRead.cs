using System.Security.Claims;
using Api.UseCases.Notifications.MarkRead;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Notifications;

public class MarkNotificationReadEndpoint(IMediator mediator)
    : Ep.Req<MarkNotificationReadRequest>.NoRes
{
    public override void Configure()
    {
        Put("/api/admin/notifications/{id}/read");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Notifications"));
    }

    public override async Task HandleAsync(MarkNotificationReadRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new MarkNotificationReadCommand(req.Id, userId), ct);
        await this.SendResultAsync(result, ct);
    }
}

public class MarkNotificationReadRequest
{
    public int Id { get; set; }
}

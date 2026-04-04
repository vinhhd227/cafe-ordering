using System.Security.Claims;
using Api.UseCases.Notifications.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Notifications;

public class DeleteNotificationEndpoint(IMediator mediator)
    : Ep.Req<DeleteNotificationRequest>.NoRes
{
    public override void Configure()
    {
        Delete("/api/admin/notifications/{id}");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Notifications"));
    }

    public override async Task HandleAsync(DeleteNotificationRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new DeleteNotificationCommand(req.Id, userId), ct);
        await this.SendResultAsync(result, ct);
    }
}

public class DeleteNotificationRequest
{
    public int Id { get; set; }
}

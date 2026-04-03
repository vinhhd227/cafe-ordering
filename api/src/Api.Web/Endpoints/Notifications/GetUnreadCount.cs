using System.Security.Claims;
using Api.UseCases.Notifications.GetUnreadCount;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Notifications;

public class GetUnreadCountEndpoint(IMediator mediator) : EndpointWithoutRequest<int>
{
    public override void Configure()
    {
        Get("/api/admin/notifications/unread-count");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new GetUnreadCountQuery(userId), ct);
        await this.SendResultAsync(result, ct);
    }
}

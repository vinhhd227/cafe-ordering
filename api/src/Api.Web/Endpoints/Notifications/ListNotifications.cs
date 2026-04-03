using System.Security.Claims;
using Api.UseCases.Notifications.DTOs;
using Api.UseCases.Notifications.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Notifications;

public class ListNotificationsEndpoint(IMediator mediator)
    : Endpoint<ListNotificationsRequest, NotificationListDto>
{
    public override void Configure()
    {
        Get("/api/admin/notifications");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Notifications"));
    }

    public override async Task HandleAsync(ListNotificationsRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new ListNotificationsQuery(userId, req.Page, req.PageSize), ct);
        await this.SendResultAsync(result, ct);
    }
}

public class ListNotificationsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

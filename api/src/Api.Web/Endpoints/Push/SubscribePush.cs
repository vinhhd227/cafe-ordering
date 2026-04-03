using System.Security.Claims;
using Api.UseCases.Push.Subscribe;
using Api.Web.Extensions;
using FastEndpoints;
using Mediator;

namespace Api.Web.Endpoints.Push;

public class SubscribePushRequest
{
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}

public class SubscribePushEndpoint(IMediator mediator) : Endpoint<SubscribePushRequest>
{
    public override void Configure()
    {
        Post("/api/admin/push/subscribe");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Push"));
    }

    public override async Task HandleAsync(SubscribePushRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new SubscribePushCommand(userId, req.Endpoint, req.P256dh, req.Auth), ct);
        await this.SendResultAsync(result, ct);
    }
}

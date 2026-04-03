using Api.UseCases.Push.Unsubscribe;
using Api.Web.Extensions;
using FastEndpoints;
using Mediator;

namespace Api.Web.Endpoints.Push;

public class UnsubscribePushRequest
{
    public string Endpoint { get; set; } = default!;
}

public class UnsubscribePushEndpoint(IMediator mediator) : Endpoint<UnsubscribePushRequest>
{
    public override void Configure()
    {
        Delete("/api/admin/push/subscribe");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Push"));
    }

    public override async Task HandleAsync(UnsubscribePushRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new UnsubscribePushCommand(req.Endpoint), ct);
        await this.SendResultAsync(result, ct);
    }
}

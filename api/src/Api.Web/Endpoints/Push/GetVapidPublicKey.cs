using Api.Infrastructure.Services;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace Api.Web.Endpoints.Push;

public class GetVapidPublicKeyEndpoint(IOptions<VapidSettings> vapid)
    : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/api/admin/push/vapid-public-key");
        Policies("StaffOrAdmin");
        DontAutoTag();
        Description(b => b.WithTags("Push"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(vapid.Value.PublicKey, ct);
    }
}

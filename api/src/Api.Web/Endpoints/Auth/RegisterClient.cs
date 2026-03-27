using Api.UseCases.Auth.Register;
using Mediator;

namespace Api.Web.Endpoints.Auth;

public class RegisterClientEndpoint(IMediator mediator) : Ep.Req<RegisterRequest>.Res<RegisterResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/register");
        AllowAnonymous();
        DontAutoTag();
        Description(b => b.WithTags("Authentication"));
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new RegisterCommand(req.Username, req.Email, req.Password, req.FullName), ct);

        if (result.IsSuccess)
        {
            await SendOkAsync(new RegisterResponse
            {
                CustomerId = result.Value.CustomerId,
                Email = result.Value.Email
            }, ct);
        }
        else
        {
            AddError(string.Join(", ", result.Errors));
            await SendErrorsAsync(400, ct);
        }
    }
}

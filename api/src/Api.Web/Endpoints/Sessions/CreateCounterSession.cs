using Api.UseCases.Sessions.CreateCounter;
using Api.UseCases.Sessions.DTOs;
using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Sessions;

public class CreateCounterSession(IMediator mediator)
  : EndpointWithoutRequest<SessionContextDto>
{
  public override void Configure()
  {
    Post("/api/admin/sessions/counter");
    Roles("Staff", "Admin");
    DontAutoTag();
    Description(b => b.WithTags("Sessions"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new CreateCounterSessionCommand(), ct);

    if (!result.IsSuccess)
    {
      await this.SendResultAsync(
        Result<SessionContextDto>.Error(string.Join("; ", result.Errors)), ct);
      return;
    }

    await SendOkAsync(
      new SessionContextDto(result.Value, null, GuestSessionStatus.Active), ct);
  }
}

using Api.UseCases.Sessions.Close;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Sessions;

public sealed class CloseSessionRequest
{
  /// <summary>The GUID of the session to close.</summary>
  public Guid SessionId { get; set; }
}

public class CloseSession(IMediator mediator) : Endpoint<CloseSessionRequest>
{
  public override void Configure()
  {
    Put("/api/sessions/{SessionId}/close");
    Roles("Staff", "Admin");
    DontAutoTag();
    Description(b => b.WithTags("Sessions"));
  }

  public override async Task HandleAsync(CloseSessionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CloseSessionCommand(req.SessionId), ct);
    await this.SendResultAsync(result, ct);
  }
}

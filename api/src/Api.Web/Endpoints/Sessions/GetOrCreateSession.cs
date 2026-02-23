using Api.UseCases.Sessions.DTOs;
using Api.UseCases.Sessions.GetOrCreate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Sessions;

public sealed class GetOrCreateSessionRequest
{
  /// <summary>The integer ID of the table.</summary>
  public int TableId { get; set; }
}

public class GetOrCreateSession(IMediator mediator) : Endpoint<GetOrCreateSessionRequest, SessionContextDto>
{
  public override void Configure()
  {
    Get("/api/tables/{TableId}/session");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Sessions"));
  }

  public override async Task HandleAsync(GetOrCreateSessionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetOrCreateSessionCommand(req.TableId), ct);
    await this.SendResultAsync(result, ct);
  }
}

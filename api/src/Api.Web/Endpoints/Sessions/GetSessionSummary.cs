using Api.UseCases.Sessions.DTOs;
using Api.UseCases.Sessions.GetSummary;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Sessions;

public sealed class GetSessionSummaryRequest
{
  /// <summary>The GUID of the session.</summary>
  public Guid SessionId { get; set; }
}

public class GetSessionSummary(IMediator mediator) : Endpoint<GetSessionSummaryRequest, SessionSummaryDto>
{
  public override void Configure()
  {
    Get("/api/sessions/{SessionId}/summary");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Sessions"));
  }

  public override async Task HandleAsync(GetSessionSummaryRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetSessionSummaryQuery(req.SessionId), ct);
    await this.SendResultAsync(result, ct);
  }
}

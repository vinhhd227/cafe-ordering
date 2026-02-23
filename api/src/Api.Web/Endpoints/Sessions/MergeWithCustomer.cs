using Api.UseCases.Sessions.Merge;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Sessions;

public sealed class MergeWithCustomerRequest
{
  /// <summary>The GUID of the session to merge.</summary>
  public Guid SessionId { get; set; }

  /// <summary>The ID of the customer to associate with this session.</summary>
  public string CustomerId { get; set; } = string.Empty;
}

public class MergeWithCustomer(IMediator mediator) : Endpoint<MergeWithCustomerRequest>
{
  public override void Configure()
  {
    Post("/api/sessions/{SessionId}/merge");
    // No AllowAnonymous() — FastEndpoints requires auth by default
    DontAutoTag();
    Description(b => b.WithTags("Sessions"));
  }

  public override async Task HandleAsync(MergeWithCustomerRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new MergeSessionWithCustomerCommand(req.SessionId, req.CustomerId), ct);
    await this.SendResultAsync(result, ct);
  }
}

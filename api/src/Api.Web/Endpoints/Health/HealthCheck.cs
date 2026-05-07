namespace Api.Web.Endpoints.Health;

public sealed record HealthCheckResponse(
  string Status,
  DateTime Timestamp);

public sealed class HealthEndpoint : Ep.NoReq.Res<HealthCheckResponse>
{
  public override void Configure()
  {
    Get("/api/health");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("System"));
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    return Send.OkAsync(new HealthCheckResponse("Healthy", DateTime.UtcNow), ct);
  }
}

namespace Api.Web.Endpoints.Health;

/// <summary>
/// Response returned by the health check endpoint.
/// </summary>
public sealed class HealthCheckResponse
{
  /// <summary>
  /// Overall health status of the API.
  /// Always <c>"Healthy"</c> when this endpoint returns HTTP 200.
  /// </summary>
  public string Status { get; set; } = "Healthy";

  /// <summary>UTC timestamp of when the health check was evaluated.</summary>
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthCheck : Ep.NoReq.Res<HealthCheckResponse>
{
  public override void Configure()
  {
    Get("/api/health");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("System"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await SendOkAsync(new HealthCheckResponse(), ct);
  }
}

namespace Api.Web.Endpoints.Health;

public class HealthCheckResponse
{
  public string Status { get; set; } = "Healthy";
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthCheck : EndpointWithoutRequest<HealthCheckResponse>
{
  public override void Configure()
  {
    Get("/api/health");
    AllowAnonymous();
    Summary(s => s.Summary = "Health check endpoint");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await SendOkAsync(new HealthCheckResponse(), ct);
  }
}

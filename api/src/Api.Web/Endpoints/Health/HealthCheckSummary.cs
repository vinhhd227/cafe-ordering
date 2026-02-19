namespace Api.Web.Endpoints.Health;

public class HealthCheckSummary : Summary<HealthCheck>
{
  public HealthCheckSummary()
  {
    Summary = "API health check";
    Description =
      "Lightweight liveness probe that confirms the API process is running and accepting requests. " +
      "Does not verify database connectivity or downstream service availability. " +
      "Intended for load-balancer health probes and uptime monitoring tools.";

    ResponseExamples[200] = new HealthCheckResponse
    {
      Status = "Healthy",
      Timestamp = new DateTime(2026, 2, 18, 15, 0, 0, DateTimeKind.Utc)
    };

    Response<HealthCheckResponse>(200, "The API is healthy and accepting requests.");
  }
}

namespace Api.Web.Endpoints.Orders;

public class SseStreamSummary : Summary<SseStream>
{
  public SseStreamSummary()
  {
    Summary = "Subscribe to real-time order events (SSE)";
    Description =
      "Opens a persistent Server-Sent Events connection. " +
      "The server pushes 'order_created' and 'order_updated' events as they occur. " +
      "Requires Staff or Admin role. " +
      "Standard EventSource cannot be used directly since the endpoint requires a Bearer token; " +
      "use fetch() with ReadableStream instead.";

    Response(200, "SSE stream opened. Events are pushed as text/event-stream.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}

using Api.UseCases.Orders.Interfaces;

namespace Api.Web.Endpoints.Orders;

/// <summary>
///   SSE stream: giữ kết nối mở và push order events tới Kanban client.
/// </summary>
public class SseStream(IOrderSseNotifier notifier) : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/admin/orders/stream");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
    Options(b => b.WithMetadata(new SkipResponseBodyFormatterMetadata()));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var response = HttpContext.Response;
    response.ContentType = "text/event-stream";
    response.Headers["Cache-Control"] = "no-cache";
    response.Headers["X-Accel-Buffering"] = "no";   // nginx: tắt buffer để push ngay
    response.Headers["Connection"] = "keep-alive";

    // Hàm gửi một SSE event xuống response stream
    // Dùng \n (LF) rõ ràng thay vì AppendLine() để tránh \r\n trên Windows
    async Task Send(string eventName, string jsonData)
    {
      var payload = $"event: {eventName}\ndata: {jsonData}\n\n";
      await response.WriteAsync(payload, ct);
      await response.Body.FlushAsync(ct);
    }

    notifier.Register(Send);
    try
    {
      // Gửi heartbeat comment để báo hiệu kết nối thành công (SSE spec: ":" = comment)
      await response.WriteAsync(": connected\n\n", ct);
      await response.Body.FlushAsync(ct);

      // Giữ kết nối tới khi client ngắt (ct bị cancel)
      await Task.Delay(Timeout.Infinite, ct);
    }
    catch (OperationCanceledException)
    {
      // Client đã ngắt kết nối — bình thường
    }
    finally
    {
      notifier.Unregister(Send);
    }
  }
}

/// <summary>Marker để FastEndpoints bỏ qua response body formatter cho SSE.</summary>
public sealed class SkipResponseBodyFormatterMetadata { }

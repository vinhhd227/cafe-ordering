namespace Api.UseCases.Orders.Interfaces;

/// <summary>
///   Singleton service: quản lý danh sách SSE connections đang mở
///   và broadcast order events tới tất cả clients.
/// </summary>
public interface IOrderSseNotifier
{
  /// <summary>Đăng ký một SSE connection. <paramref name="send"/> nhận (eventName, jsonData).</summary>
  void Register(Func<string, string, Task> send);

  /// <summary>Hủy đăng ký khi client ngắt kết nối.</summary>
  void Unregister(Func<string, string, Task> send);

  /// <summary>Broadcast event tới tất cả connections đang mở.</summary>
  Task BroadcastAsync(string eventName, string jsonData, CancellationToken ct = default);
}

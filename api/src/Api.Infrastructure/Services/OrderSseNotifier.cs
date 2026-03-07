using System.Collections.Concurrent;
using Api.UseCases.Orders.Interfaces;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Services;

/// <summary>
///   Singleton: duy trì danh sách SSE connections đang mở và broadcast events.
/// </summary>
public class OrderSseNotifier(ILogger<OrderSseNotifier> logger) : IOrderSseNotifier
{
  // Thread-safe set of active SSE send delegates
  private readonly ConcurrentDictionary<Func<string, string, Task>, byte> _clients = new();

  public void Register(Func<string, string, Task> send)
  {
    _clients.TryAdd(send, 0);
    logger.LogDebug("SSE client registered. Total: {Count}", _clients.Count);
  }

  public void Unregister(Func<string, string, Task> send)
  {
    _clients.TryRemove(send, out _);
    logger.LogDebug("SSE client unregistered. Total: {Count}", _clients.Count);
  }

  public async Task BroadcastAsync(string eventName, string jsonData, CancellationToken ct = default)
  {
    if (_clients.IsEmpty) return;

    var dead = new List<Func<string, string, Task>>();

    foreach (var (send, _) in _clients)
    {
      if (ct.IsCancellationRequested) break;
      try
      {
        await send(eventName, jsonData);
      }
      catch (Exception ex)
      {
        // Connection đã đóng — đánh dấu để xóa
        logger.LogDebug("SSE broadcast failed for a client, removing. {Message}", ex.Message);
        dead.Add(send);
      }
    }

    foreach (var send in dead)
      _clients.TryRemove(send, out _);
  }
}

using Api.Core.Aggregates.CategoryAggregate.Events;
using Api.Core.Aggregates.ProductAggregate.Events;
using Mediator;
using Microsoft.Extensions.Caching.Memory;

namespace Api.UseCases.Menu.EventHandlers;

/// <summary>
///   Tập hợp các handlers xóa cache public menu khi có thay đổi category hoặc product.
///   Được dispatch tự động qua EventDispatchInterceptor sau SaveChanges.
/// </summary>

// ── Category events ───────────────────────────────────────────────

public class InvalidateCacheOnCategoryCreated(IMemoryCache cache)
  : INotificationHandler<CategoryCreatedEvent>
{
  public ValueTask Handle(CategoryCreatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

public class InvalidateCacheOnCategoryUpdated(IMemoryCache cache)
  : INotificationHandler<CategoryUpdatedEvent>
{
  public ValueTask Handle(CategoryUpdatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

public class InvalidateCacheOnCategoryActivated(IMemoryCache cache)
  : INotificationHandler<CategoryActivatedEvent>
{
  public ValueTask Handle(CategoryActivatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

public class InvalidateCacheOnCategoryDeactivated(IMemoryCache cache)
  : INotificationHandler<CategoryDeactivatedEvent>
{
  public ValueTask Handle(CategoryDeactivatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

// ── Product events ────────────────────────────────────────────────

public class InvalidateCacheOnProductCreated(IMemoryCache cache)
  : INotificationHandler<ProductCreatedEvent>
{
  public ValueTask Handle(ProductCreatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

public class InvalidateCacheOnProductUpdated(IMemoryCache cache)
  : INotificationHandler<ProductUpdatedEvent>
{
  public ValueTask Handle(ProductUpdatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

public class InvalidateCacheOnProductActivated(IMemoryCache cache)
  : INotificationHandler<ProductActivatedEvent>
{
  public ValueTask Handle(ProductActivatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

public class InvalidateCacheOnProductDeactivated(IMemoryCache cache)
  : INotificationHandler<ProductDeactivatedEvent>
{
  public ValueTask Handle(ProductDeactivatedEvent notification, CancellationToken ct)
  {
    cache.Remove(MenuCacheKeys.PublicMenu);
    return ValueTask.CompletedTask;
  }
}

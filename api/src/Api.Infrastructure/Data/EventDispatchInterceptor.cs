using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.Infrastructure.Data;

/// <summary>
///   EF Core interceptor: dispatch domain events sau khi SaveChanges thành công.
///   Tách concern ra khỏi DbContext.
/// </summary>
public class EventDispatchInterceptor(IMediator mediator) : SaveChangesInterceptor
{
  public override async ValueTask<int> SavedChangesAsync(
    SaveChangesCompletedEventData eventData,
    int result,
    CancellationToken ct = default)
  {
    if (eventData.Context is not null)
    {
      await DispatchDomainEventsAsync(eventData.Context, ct);
    }

    return await base.SavedChangesAsync(eventData, result, ct);
  }

  private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
  {
    var entitiesWithEvents = context.ChangeTracker
      .Entries<IHasDomainEvents>()
      .Where(e => e.Entity.DomainEvents.Any()) 
      .Select(e => e.Entity)
      .ToList();

    if (entitiesWithEvents.Count == 0)
      return;

    // Collect all events
    var domainEvents = entitiesWithEvents
      .SelectMany(e => e.DomainEvents)
      .ToList();

    // Clear events (Ardalis EntityBase có ClearDomainEvents protected)
    // Workaround: Cast sang EntityBase
    foreach (var entity in entitiesWithEvents)
    {
      if (entity is HasDomainEventsBase baseEntity)
      {
        baseEntity.ClearDomainEvents();
      }
    }

    // Dispatch events
    foreach (var domainEvent in domainEvents)
    {
      await mediator.Publish(domainEvent, ct);
    }
  }
}

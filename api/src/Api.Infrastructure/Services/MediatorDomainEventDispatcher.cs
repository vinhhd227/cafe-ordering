// namespace Api.Infrastructure.Services;
//
// /// <summary>
// ///   Dispatch domain events qua MediatR.Publish()
// /// </summary>
// public class MediatorDomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
// {
//   public async Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken ct = default)
//   {
//     foreach (var entity in entities)
//     {
//       var events = entity.DomainEvents.ToArray();
//       entity.ClearDomainEvents();
//
//       foreach (var domainEvent in events)
//       {
//         await mediator.Publish(domainEvent, ct);
//       }
//     }
//   }
// }

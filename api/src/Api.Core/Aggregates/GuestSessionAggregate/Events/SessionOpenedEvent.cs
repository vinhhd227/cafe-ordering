namespace Api.Core.Aggregates.GuestSessionAggregate.Events;

public class SessionOpenedEvent(Guid sessionId, int tableId) : DomainEventBase
{
  public Guid SessionId { get; } = sessionId;
  public int TableId { get; } = tableId;
}

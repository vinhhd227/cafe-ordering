namespace Api.Core.Aggregates.GuestSessionAggregate.Events;

public class SessionClosedEvent(Guid sessionId, int tableId) : DomainEventBase
{
  public Guid SessionId { get; } = sessionId;
  public int TableId { get; } = tableId;
}

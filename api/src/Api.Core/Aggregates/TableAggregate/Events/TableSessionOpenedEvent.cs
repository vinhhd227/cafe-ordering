namespace Api.Core.Aggregates.TableAggregate.Events;

public class TableSessionOpenedEvent(int tableId, Guid sessionId) : DomainEventBase
{
  public int TableId { get; } = tableId;
  public Guid SessionId { get; } = sessionId;
}

namespace Api.Core.Aggregates.TableAggregate.Events;

public class TableSessionClosedEvent(int tableId) : DomainEventBase
{
  public int TableId { get; } = tableId;
}

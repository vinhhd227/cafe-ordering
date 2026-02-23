namespace Api.Core.Aggregates.GuestSessionAggregate.Events;

public class SessionMergedWithCustomerEvent(Guid sessionId, string customerId) : DomainEventBase
{
  public Guid SessionId { get; } = sessionId;
  public string CustomerId { get; } = customerId;
}

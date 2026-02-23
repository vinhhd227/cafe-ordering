using Api.Core.Aggregates.GuestSessionAggregate.Events;

namespace Api.Core.Aggregates.GuestSessionAggregate;

public class GuestSession : AuditableEntity<Guid>, IAggregateRoot
{
  private GuestSession() { }

  public int TableId { get; private set; }
  public GuestSessionStatus Status { get; private set; }
  public DateTime OpenedAt { get; private set; }
  public DateTime? ClosedAt { get; private set; }
  public string? CustomerId { get; private set; }

  public static GuestSession Create(int tableId)
  {
    Guard.Against.NegativeOrZero(tableId, nameof(tableId));

    var session = new GuestSession
    {
      Id = Guid.NewGuid(),
      TableId = tableId,
      Status = GuestSessionStatus.Active,
      OpenedAt = DateTime.UtcNow
    };

    session.RegisterDomainEvent(new SessionOpenedEvent(session.Id, tableId));
    return session;
  }

  public void Close()
  {
    if (Status == GuestSessionStatus.Closed)
      throw new InvalidOperationException("Session is already closed.");

    Status = GuestSessionStatus.Closed;
    ClosedAt = DateTime.UtcNow;
    RegisterDomainEvent(new SessionClosedEvent(Id, TableId));
  }

  public void MergeWithCustomer(string customerId)
  {
    Guard.Against.NullOrEmpty(customerId, nameof(customerId));

    if (Status == GuestSessionStatus.Closed)
      throw new InvalidOperationException("Cannot merge a closed session.");

    CustomerId = customerId;
    RegisterDomainEvent(new SessionMergedWithCustomerEvent(Id, customerId));
  }
}

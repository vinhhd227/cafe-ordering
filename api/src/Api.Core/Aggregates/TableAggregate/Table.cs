using Api.Core.Aggregates.TableAggregate.Events;

namespace Api.Core.Aggregates.TableAggregate;

public class Table : SoftDeletableEntity<int>, IAggregateRoot
{
  private Table() { }

  public int Number { get; private set; }
  public bool IsActive { get; private set; } = true;
  public TableStatus Status { get; private set; } = TableStatus.Available;
  public Guid? ActiveSessionId { get; private set; }

  public static Table Create(int number)
  {
    var table = new Table
    {
      Number = Guard.Against.NegativeOrZero(number),
      IsActive = true,
      Status = TableStatus.Available
    };

    return table;
  }

  public void UpdateNumber(int number)
  {
    Number = Guard.Against.NegativeOrZero(number);
  }

  public void Activate()
  {
    IsActive = true;
  }

  public void Deactivate()
  {
    IsActive = false;
  }

  public void OpenSession(Guid sessionId)
  {
    if (Status == TableStatus.Occupied)
      throw new InvalidOperationException($"Table {Number} already has an active session.");

    Status = TableStatus.Occupied;
    ActiveSessionId = sessionId;
    RegisterDomainEvent(new TableSessionOpenedEvent(Id, sessionId));
  }

  public void CloseSession()
  {
    Status = TableStatus.Cleaning;
    ActiveSessionId = null;
    RegisterDomainEvent(new TableSessionClosedEvent(Id));
  }

  public void MarkAvailable()
  {
    Status = TableStatus.Available;
  }
}

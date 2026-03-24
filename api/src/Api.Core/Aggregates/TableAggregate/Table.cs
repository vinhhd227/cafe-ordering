using Api.Core.Aggregates.TableAggregate.Events;
using Api.Core.Aggregates.ZoneAggregate;

namespace Api.Core.Aggregates.TableAggregate;

public class Table : SoftDeletableEntity<int>, IAggregateRoot
{
  private Table() { }

  public string Code { get; private set; } = string.Empty;
  public bool IsActive { get; private set; } = true;
  public TableStatus Status { get; private set; } = TableStatus.Available;
  public Guid? ActiveSessionId { get; private set; }
  public int? ZoneId { get; private set; }
  public Zone? Zone { get; private set; }
  public Guid QrToken { get; private set; } = Guid.NewGuid();

  public static Table Create(string code, int? zoneId = null)
  {
    var table = new Table
    {
      Code     = Guard.Against.NullOrWhiteSpace(code),
      IsActive = true,
      Status   = TableStatus.Available,
      ZoneId   = zoneId,
      QrToken  = Guid.NewGuid()
    };

    return table;
  }

  public void AssignZone(int? zoneId)
  {
    ZoneId = zoneId;
  }

  public void UpdateCode(string code)
  {
    Code = Guard.Against.NullOrWhiteSpace(code);
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
      throw new InvalidOperationException($"Table {Code} already has an active session.");

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

  public void RegenerateQrToken()
  {
    QrToken = Guid.NewGuid();
  }
}

namespace Api.Core.Aggregates.ZoneAggregate;

public class Zone : SoftDeletableEntity<int>, IAggregateRoot
{
  private Zone() { }

  public string Name { get; private set; } = string.Empty;
  public int DisplayOrder { get; private set; }
  public bool IsActive { get; private set; } = true;

  public static Zone Create(string name, int displayOrder = 0)
  {
    return new Zone
    {
      Name         = Guard.Against.NullOrWhiteSpace(name),
      DisplayOrder = displayOrder,
      IsActive     = true
    };
  }

  public void UpdateName(string name)
  {
    Name = Guard.Against.NullOrWhiteSpace(name);
  }

  public void SetDisplayOrder(int order)
  {
    DisplayOrder = order;
  }

  public void Activate()
  {
    IsActive = true;
  }

  public void Deactivate()
  {
    IsActive = false;
  }
}

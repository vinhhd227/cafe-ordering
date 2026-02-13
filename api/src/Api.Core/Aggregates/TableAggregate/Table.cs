namespace Api.Core.Aggregates.TableAggregate;

public class Table : SoftDeletableEntity<int>, IAggregateRoot
{
  private Table() { }
  public int Number { get; private set; }
  public bool IsActive { get; private set; } = true;

  public static Table Create(int number)
  {
    var table = new Table { Number = Guard.Against.NegativeOrZero(number), IsActive = true };

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
}

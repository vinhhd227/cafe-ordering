using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.ExpenseAggregate;

public class ExpenseCategory : SmartEnum<ExpenseCategory>
{
  public static readonly ExpenseCategory Ingredient = new("INGREDIENT", 1);
  public static readonly ExpenseCategory Supply     = new("SUPPLY",     2);
  public static readonly ExpenseCategory Equipment  = new("EQUIPMENT",  3);
  public static readonly ExpenseCategory Other      = new("OTHER",      4);

  private ExpenseCategory(string name, int value) : base(name, value) { }
}

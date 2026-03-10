using Api.Core.Aggregates.ExpenseAggregate.Events;
using Api.Core.Aggregates.OrderAggregate;

namespace Api.Core.Aggregates.ExpenseAggregate;

public class Expense : SoftDeletableEntity<int>, IAggregateRoot
{
  private Expense() { }

  public string Name { get; private set; } = string.Empty;
  public ExpenseCategory Category { get; private set; } = ExpenseCategory.Other;
  public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.Cash;
  public decimal Quantity { get; private set; }
  public string? Unit { get; private set; }
  public decimal UnitPrice { get; private set; }
  public decimal TotalAmount { get; private set; }
  public DateTime PurchaseDate { get; private set; }
  public string? Notes { get; private set; }

  public static Expense Create(
    string name,
    ExpenseCategory category,
    PaymentMethod paymentMethod,
    decimal quantity,
    string? unit,
    decimal unitPrice,
    DateTime purchaseDate,
    string? notes = null)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Guard.Against.NegativeOrZero(quantity);
    Guard.Against.Negative(unitPrice);

    var expense = new Expense
    {
      Name          = name.Trim(),
      Category      = category,
      PaymentMethod = paymentMethod,
      Quantity      = quantity,
      Unit          = unit?.Trim() is { Length: > 0 } u ? u : null,
      UnitPrice     = unitPrice,
      TotalAmount   = quantity * unitPrice,
      PurchaseDate  = purchaseDate,
      Notes         = notes?.Trim() is { Length: > 0 } n ? n : null,
    };

    expense.RegisterDomainEvent(new ExpenseCreatedEvent(expense));

    return expense;
  }

  public void Update(
    string name,
    ExpenseCategory category,
    PaymentMethod paymentMethod,
    decimal quantity,
    string? unit,
    decimal unitPrice,
    DateTime purchaseDate,
    string? notes)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Guard.Against.NegativeOrZero(quantity);
    Guard.Against.Negative(unitPrice);

    Name          = name.Trim();
    Category      = category;
    PaymentMethod = paymentMethod;
    Quantity      = quantity;
    Unit          = unit?.Trim() is { Length: > 0 } u ? u : null;
    UnitPrice     = unitPrice;
    TotalAmount   = quantity * unitPrice;
    PurchaseDate  = purchaseDate;
    Notes         = notes?.Trim() is { Length: > 0 } n ? n : null;

    RegisterDomainEvent(new ExpenseUpdatedEvent(this));
  }
}

namespace Api.Core.Aggregates.ExpenseAggregate.Events;

/// <summary>
///   Event khi Expense được cập nhật
/// </summary>
public class ExpenseUpdatedEvent(Expense expense) : DomainEventBase
{
  public int ExpenseId { get; } = expense.Id;
  public string ExpenseName { get; } = expense.Name;
  public decimal TotalAmount { get; } = expense.TotalAmount;
}

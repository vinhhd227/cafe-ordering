namespace Api.Core.Aggregates.ExpenseAggregate.Events;

/// <summary>
///   Event khi Expense được tạo mới
/// </summary>
public class ExpenseCreatedEvent(Expense expense) : DomainEventBase
{
  public int ExpenseId { get; } = expense.Id;
  public string ExpenseName { get; } = expense.Name;
  public decimal TotalAmount { get; } = expense.TotalAmount;
}

namespace Api.UseCases.Expenses.Update;

public record UpdateExpenseCommand(
  int ExpenseId,
  string Name,
  string Category,
  string PaymentMethod,
  decimal Quantity,
  string? Unit,
  decimal UnitPrice,
  DateTime PurchaseDate,
  string? Notes,
  int? TotalAmount = null
) : ICommand<Result>;

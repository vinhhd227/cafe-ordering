using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.Create;

public record CreateExpenseCommand(
  string Name,
  string Category,
  string PaymentMethod,
  decimal Quantity,
  string? Unit,
  decimal UnitPrice,
  DateTime PurchaseDate,
  string? Notes,
  int? TotalAmount = null
) : ICommand<Result<ExpenseDto>>;

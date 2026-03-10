namespace Api.UseCases.Expenses.DTOs;

public record ExpenseDto(
  int Id,
  string Name,
  string Category,
  string PaymentMethod,
  decimal Quantity,
  string? Unit,
  decimal UnitPrice,
  decimal TotalAmount,
  DateTime PurchaseDate,
  string? Notes,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);

public record PagedExpensesDto(
  List<ExpenseDto> Items,
  int TotalCount,
  int Page,
  int PageSize,
  decimal GrandTotal
);

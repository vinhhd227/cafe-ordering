using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.OrderAggregate;
using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.Create;

public class CreateExpenseHandler(IRepositoryBase<Expense> repository)
  : ICommandHandler<CreateExpenseCommand, Result<ExpenseDto>>
{
  public async ValueTask<Result<ExpenseDto>> Handle(CreateExpenseCommand request, CancellationToken ct)
  {
    var category      = ExpenseCategory.FromName(request.Category, true);
    var paymentMethod = PaymentMethod.FromName(request.PaymentMethod, true);
    var purchaseDate  = DateTime.SpecifyKind(request.PurchaseDate, DateTimeKind.Utc);

    var expense = Expense.Create(
      request.Name,
      category,
      paymentMethod,
      request.Quantity,
      request.Unit,
      request.UnitPrice,
      purchaseDate,
      request.Notes,
      request.TotalAmount);

    await repository.AddAsync(expense, ct);

    return Result.Success(ToDto(expense));
  }

  internal static ExpenseDto ToDto(Expense e) => new(
    e.Id,
    e.Name,
    e.Category.Name,
    e.PaymentMethod.Name,
    e.Quantity,
    e.Unit,
    e.UnitPrice,
    e.TotalAmount,
    e.PurchaseDate,
    e.Notes,
    e.CreatedAt,
    e.UpdatedAt);
}

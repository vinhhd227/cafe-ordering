using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.ExpenseAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;

namespace Api.UseCases.Expenses.Update;

public class UpdateExpenseHandler(IRepositoryBase<Expense> repository)
  : ICommandHandler<UpdateExpenseCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateExpenseCommand request, CancellationToken ct)
  {
    var expense = await repository.FirstOrDefaultAsync(new ExpenseByIdSpec(request.ExpenseId), ct);

    if (expense is null)
      return Result.NotFound($"Expense {request.ExpenseId} not found");

    var category      = ExpenseCategory.FromName(request.Category, true);
    var paymentMethod = PaymentMethod.FromName(request.PaymentMethod, true);
    var purchaseDate  = DateTime.SpecifyKind(request.PurchaseDate, DateTimeKind.Utc);

    expense.Update(
      request.Name,
      category,
      paymentMethod,
      request.Quantity,
      request.Unit,
      request.UnitPrice,
      purchaseDate,
      request.Notes,
      request.TotalAmount);

    await repository.UpdateAsync(expense, ct);

    return Result.Success();
  }
}

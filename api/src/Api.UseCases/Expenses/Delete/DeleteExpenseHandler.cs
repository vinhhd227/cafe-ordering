using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.ExpenseAggregate.Specifications;

namespace Api.UseCases.Expenses.Delete;

public class DeleteExpenseHandler(IRepositoryBase<Expense> repository)
  : ICommandHandler<DeleteExpenseCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteExpenseCommand request, CancellationToken ct)
  {
    var expense = await repository.FirstOrDefaultAsync(new ExpenseByIdSpec(request.ExpenseId), ct);

    if (expense is null)
      return Result.NotFound($"Expense {request.ExpenseId} not found");

    expense.Delete(request.DeletedBy);

    await repository.UpdateAsync(expense, ct);

    return Result.Success();
  }
}

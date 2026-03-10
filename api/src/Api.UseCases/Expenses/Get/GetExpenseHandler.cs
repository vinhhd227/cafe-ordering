using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.ExpenseAggregate.Specifications;
using Api.UseCases.Expenses.Create;
using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.Get;

public class GetExpenseHandler(IReadRepositoryBase<Expense> repository)
  : IQueryHandler<GetExpenseQuery, Result<ExpenseDto>>
{
  public async ValueTask<Result<ExpenseDto>> Handle(GetExpenseQuery request, CancellationToken ct)
  {
    var expense = await repository.FirstOrDefaultAsync(new ExpenseByIdSpec(request.ExpenseId), ct);

    if (expense is null)
      return Result.NotFound($"Expense {request.ExpenseId} not found");

    return Result.Success(CreateExpenseHandler.ToDto(expense));
  }
}

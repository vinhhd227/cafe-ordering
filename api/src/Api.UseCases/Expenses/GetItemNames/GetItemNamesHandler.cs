using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.ExpenseAggregate.Specifications;

namespace Api.UseCases.Expenses.GetItemNames;

public class GetItemNamesHandler(IReadRepositoryBase<Expense> repository)
  : IQueryHandler<GetItemNamesQuery, Result<List<string>>>
{
  public async ValueTask<Result<List<string>>> Handle(GetItemNamesQuery query, CancellationToken ct)
  {
    var names = await repository.ListAsync(new ExpenseItemNamesSpec(query.Query), ct);

    var suggestions = names
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
      .Take(20)
      .ToList();

    return Result.Success(suggestions);
  }
}

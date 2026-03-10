using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.List;

public record ListExpensesQuery(
  string? Category = null,
  DateTime? DateFrom = null,
  DateTime? DateTo = null,
  int Page = 1,
  int PageSize = 20
) : IQuery<Result<PagedExpensesDto>>;

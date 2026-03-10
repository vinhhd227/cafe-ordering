using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.GetSummary;

public record GetExpenseSummaryQuery(
  DateTime? DateFrom = null,
  DateTime? DateTo = null
) : IQuery<Result<ExpenseSummaryDto>>;

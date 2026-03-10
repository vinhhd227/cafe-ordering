using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.Get;

public record GetExpenseQuery(int ExpenseId) : IQuery<Result<ExpenseDto>>;

namespace Api.UseCases.Expenses.Delete;

public record DeleteExpenseCommand(int ExpenseId, string DeletedBy) : ICommand<Result>;

namespace Api.UseCases.Expenses.GetItemNames;

public record GetItemNamesQuery(string? Query) : IQuery<Result<List<string>>>;

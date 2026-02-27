namespace Api.UseCases.Tables.Delete;

public record DeleteTableCommand(int TableId, string DeletedBy) : ICommand<Result>;

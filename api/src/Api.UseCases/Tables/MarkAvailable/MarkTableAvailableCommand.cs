namespace Api.UseCases.Tables.MarkAvailable;

public record MarkTableAvailableCommand(int TableId) : ICommand<Result>;

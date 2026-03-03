namespace Api.UseCases.Tables.Activate;

public record ActivateTableCommand(int TableId) : ICommand<Result>;

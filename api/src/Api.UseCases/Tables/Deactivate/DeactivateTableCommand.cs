namespace Api.UseCases.Tables.Deactivate;

public record DeactivateTableCommand(int TableId) : ICommand<Result>;

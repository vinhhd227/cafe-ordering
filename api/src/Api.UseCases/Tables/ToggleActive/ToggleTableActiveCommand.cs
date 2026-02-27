namespace Api.UseCases.Tables.ToggleActive;

public record ToggleTableActiveCommand(int TableId) : ICommand<Result>;

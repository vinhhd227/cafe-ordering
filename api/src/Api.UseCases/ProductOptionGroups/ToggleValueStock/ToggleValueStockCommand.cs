namespace Api.UseCases.ProductOptionGroups.ToggleValueStock;

public record ToggleValueStockCommand(int GroupId, int ValueId) : ICommand<Result>;

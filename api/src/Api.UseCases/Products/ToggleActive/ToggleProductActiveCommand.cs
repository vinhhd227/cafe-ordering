namespace Api.UseCases.Products.ToggleActive;

public record ToggleProductActiveCommand(int ProductId) : ICommand<Result>;

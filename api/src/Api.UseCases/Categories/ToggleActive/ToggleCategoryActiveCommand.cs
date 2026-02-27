namespace Api.UseCases.Categories.ToggleActive;

public record ToggleCategoryActiveCommand(int CategoryId) : ICommand<Result>;

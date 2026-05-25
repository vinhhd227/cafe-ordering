namespace Api.UseCases.Products.UnassignCategory;

public record UnassignCategoryCommand(int ProductId) : ICommand<Result>;

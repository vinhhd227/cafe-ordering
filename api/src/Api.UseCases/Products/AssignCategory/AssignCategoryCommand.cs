namespace Api.UseCases.Products.AssignCategory;

public record AssignCategoryCommand(int ProductId, int CategoryId) : ICommand<Result>;

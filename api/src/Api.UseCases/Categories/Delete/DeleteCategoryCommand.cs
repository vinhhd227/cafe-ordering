namespace Api.UseCases.Categories.Delete;

/// <summary>
///   Command soft delete Category
/// </summary>
public record DeleteCategoryCommand(int CategoryId, string DeletedBy) : ICommand<Result>;

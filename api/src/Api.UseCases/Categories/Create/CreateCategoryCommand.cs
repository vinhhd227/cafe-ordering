namespace Api.UseCases.Categories.Create;

/// <summary>
///   Command tạo mới Category
/// </summary>
public record CreateCategoryCommand(string Name) : Common.Interfaces.ICommand<Result<int>>;
